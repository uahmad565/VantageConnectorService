﻿using System.Diagnostics;
using System.DirectoryServices;
using System.Text;
using System.Text.Json;
using ActiveDirectorySearcher.DTOs;
using CommonUtils;
using CommonUtils.GlobalObjects;
using Newtonsoft.Json;

namespace ActiveDirectorySearcher;

#pragma warning disable CA1416 //suppress windows warning 
public class ActiveDirectoryHelper
{
    private static Dictionary<string, string> keyValuePairs = new();

    public static void LoadOUReplication()
    {
        var filePath = Path.Combine(GlobalFileHandler.InfoDirectory, GlobalFileHandler.OU_UserGroupsReplicationFileName);

        string fileJson = File.ReadAllText(filePath);
        if (!string.IsNullOrEmpty(fileJson))
        {
            keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileJson) ?? new Dictionary<string, string>();
        }
    }

    public static async Task WriteOUReplication()
    {
        var filePath = Path.Combine(GlobalFileHandler.InfoDirectory, GlobalFileHandler.OU_UserGroupsReplicationFileName);
        var json = await SerializerHelper.GetSerializedObject(keyValuePairs);
        await File.WriteAllTextAsync(filePath, json);
    }

    public static async Task ProcessADObjects(InputCreds inputCreds, IProgress<Status>? progress, ObjectType objectType, ICollection<string> containers, int recordsToSyncInSingleRequest, CancellationToken cancellationToken)
    {
        if (containers.Count > 0)
        {
            foreach (var container in containers)
            {
                var currReplicationTime = DateTime.Now.ToUniversalTime().ToString();
                string? lastReplicationTime = "";

                if (keyValuePairs.ContainsKey($"{container}_{objectType}"))
                    lastReplicationTime = keyValuePairs[$"{container}_{objectType}"];

                await ProcessADObjects(inputCreds, progress, objectType, cancellationToken, lastReplicationTime, recordsToSyncInSingleRequest, container);
                keyValuePairs[$"{container}_{objectType}"] = currReplicationTime;
            }
        }
        else
        {
            string filePath = objectType switch
            {
                ObjectType.User => Path.Combine(GlobalFileHandler.InfoDirectory, GlobalFileHandler.UserReplicationFileName),
                ObjectType.Group => Path.Combine(GlobalFileHandler.InfoDirectory, GlobalFileHandler.GroupReplicationFileName),
                ObjectType.OU => Path.Combine(GlobalFileHandler.InfoDirectory, GlobalFileHandler.OUReplicationFileName),
                _ => ""
            };
            //*tobe Add Info folder if it doesn't exist
            var currReplicationTime = DateTime.Now.ToUniversalTime().ToString();
            var lastReplicationTime = await File.ReadAllTextAsync(filePath);
            await ProcessADObjects(inputCreds, progress, objectType, cancellationToken, lastReplicationTime, recordsToSyncInSingleRequest);
            await File.WriteAllTextAsync(filePath, currReplicationTime, cancellationToken);
        }
    }
    private static async Task ProcessADObjects(InputCreds inputCreds, IProgress<Status>? progress, ObjectType objectType, CancellationToken cancellationToken, string? lastReplicationTime, int recordsToSyncInSingleRequest, string ouPath = "")
    {
        progress?.Report(new($"Processing {objectType} {ouPath}. {Environment.NewLine}", ""));

        var whenChangedFilter = string.IsNullOrEmpty(lastReplicationTime) ? "" : DateTime.Parse(lastReplicationTime).ToString("yyyyMMddHHmmss.0Z");
        var objectsList = new List<SearchResult>();
        using var root = await GetRootEntry(inputCreds, ouPath);

        using var searcher = new DirectorySearcher(root);
        searcher.Filter = PrepareLdapQuery(objectType, whenChangedFilter);
        searcher.PageSize = 500;
        searcher.SizeLimit = 0;

        using var results = await Task.Run(() => searcher?.FindAll());
        var resultsEnumerator = results?.GetEnumerator();

        if (resultsEnumerator != null)
        {
            var dnList = new List<string>();
            int i = 0;
            for (; resultsEnumerator.MoveNext(); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = (SearchResult)resultsEnumerator.Current;
                objectsList.Add(result);
                var distinguishedName = result.Properties["distinguishedName"][0] as string ?? "";
                dnList.Add(distinguishedName);

                if ((i + 1) % recordsToSyncInSingleRequest == 0)
                    ReportFetchObjects(objectType, dnList, i + 1, progress);

                if ((i + 1) % recordsToSyncInSingleRequest == 0)
                    await SendObjectListToWebService(inputCreds.Host, inputCreds.DomainId, objectsList, objectType, progress, cancellationToken);
            }
            if (dnList.Count > 0)
                ReportFetchObjects(objectType, dnList, i, progress);

            if (objectsList.Count > 0)
                await SendObjectListToWebService(inputCreds.Host, inputCreds.DomainId, objectsList, objectType, progress, cancellationToken);


        }
    }

    public static async Task<DirectoryEntry> GetRootEntry(InputCreds inputCreds, string ouPath)
    {
        var entry = await Task.Run(() =>
        {
            var path = new StringBuilder($"LDAP://{inputCreds.Domain}{(inputCreds.Port is 0 ? "" : $":{inputCreds.Port}")}");
            path.Append(ouPath != "" ? $"/{ouPath}" : "");
            var root = string.IsNullOrEmpty(inputCreds.UserName) ? new DirectoryEntry(path.ToString()) : new DirectoryEntry(path.ToString(), inputCreds.UserName, inputCreds.Password);
            _ = root.Name; // checking connection; will throw if connection is not succesful
            return root;
        });

        return entry;
    }

    #region Private static helper methods
    private static async Task SendObjectListToWebService(string host, string domainID, List<SearchResult> objectsList, ObjectType objectType, IProgress<Status>? progress, CancellationToken cancellationToken)
    {
        progress?.Report(new("", SendingObjectsRequestMessage(objectsList.Count, objectType)));

        var json = await SerializerHelper.GetSerializedObject(objectsList);
        string apiUrl = objectType switch
        {
            ObjectType.User => $"{host}/active-directory/sync-data?domainId={domainID}&type=user",
            ObjectType.Group => $"{host}/active-directory/sync-data?domainId={domainID}&type=group",
            ObjectType.OU => $"{host}/active-directory/sync-data?domainId={domainID}&type=ou",
            _ => ""
        };
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(30);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        Stopwatch sw = new();
        sw.Start();
        response = await client.PutAsync(apiUrl, content, cancellationToken);
        sw.Stop();
        progress?.Report(new("", "Response Time Elapsed: " + sw.Elapsed + Environment.NewLine));
        // Check the response status
        if (!response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            progress?.Report(new("", $"Request failed with status code {response.StatusCode} and ResponseBody {responseBody}"));
            throw new Exception(responseBody);
        }
        objectsList.Clear();
    }
    private static string PrepareLdapQuery(ObjectType objectType, string whenChangedFilter)
    {
        string ldapfilter = objectType switch
        {
            ObjectType.User => string.IsNullOrEmpty(whenChangedFilter) ? $"(objectClass=user)" : $"(&(objectClass=user)(whenChanged>={whenChangedFilter}))",
            ObjectType.Group => string.IsNullOrEmpty(whenChangedFilter) ? "(objectClass=group)" : $"(&(objectClass=group)(whenChanged>={whenChangedFilter}))",
            ObjectType.OU => "(objectClass=organizationalUnit)",
            _ => ""
        };

        return ldapfilter;
    }
    private static string FetchObjectsMessage(ObjectType objectType, List<string> list)
    {
        var sb = new StringBuilder();
        list.ForEach(x => sb.Append($"fetch {objectType} {x}.{Environment.NewLine}"));
        return sb.ToString();
    }

    private static string SendingObjectsRequestMessage(int count, ObjectType objectType)
    {
        return $"Sending ${count} {objectType}s request.{Environment.NewLine}";
    }

    private static void ReportFetchObjects(ObjectType objectType, List<string> dnList, int i, IProgress<Status>? progress)
    {
        progress?.Report(new($"{FetchObjectsMessage(objectType, dnList)} id: {i} {Environment.NewLine}", ""));
        dnList.Clear();
    }

    #endregion


}
#pragma warning restore CA1416