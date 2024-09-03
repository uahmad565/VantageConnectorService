using System.Diagnostics;
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

    public static Dictionary<string, string> LoadOUReplication()
    {
        Dictionary<string, string> keyValuePairs = new();
        var filePath = Path.Combine(GlobalFileHandler.InfoDirectory, GlobalFileHandler.OU_UserGroupsReplicationFileName);

        string fileJson = File.ReadAllText(filePath);
        if (!string.IsNullOrEmpty(fileJson))
        {
            keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileJson) ?? new Dictionary<string, string>();
            return keyValuePairs;
        }
        return keyValuePairs;
    }

    public static async Task WriteOUReplication(Dictionary<string, string> keyValuePairs)
    {
        var filePath = Path.Combine(GlobalFileHandler.InfoDirectory, GlobalFileHandler.OU_UserGroupsReplicationFileName);
        var json = await SerializerHelper.GetSerializedObject(keyValuePairs);
        await File.WriteAllTextAsync(filePath, json);
    }

    public static async Task ProcessADObjects(InputCreds inputCreds, IProgress<Status>? progress, ObjectType objectType, ICollection<string> containers, int recordsToSyncInSingleRequest, CancellationToken cancellationToken)
    {
        if (containers.Count > 0)
        {
            Dictionary<string, string> keyValuePairs = LoadOUReplication();
            foreach (var container in containers)
            {
                var currReplicationTime = DateTime.Now.ToUniversalTime().ToString();
                string? lastReplicationTime = "";

                if (keyValuePairs.ContainsKey($"{container}_{objectType}"))
                    lastReplicationTime = keyValuePairs[$"{container}_{objectType}"];

                await ProcessADObjects(inputCreds, progress, objectType, cancellationToken, lastReplicationTime, recordsToSyncInSingleRequest, container);
                keyValuePairs[$"{container}_{objectType}"] = currReplicationTime;
                await WriteOUReplication(keyValuePairs);
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
    #region Delete Replication 
    public static async Task ProcessDeleteADObjects(InputCreds inputCreds, IProgress<Status>? progress, ObjectType objectType, int recordsToSyncInSingleRequest, CancellationToken cancellationToken)
    {
        string filePath = objectType switch
        {
            ObjectType.User => Path.Combine(DeleteReplicationGlobalFileHandler.DeleteInfoDirectory, DeleteReplicationGlobalFileHandler.DeleteUserReplicationFileName),
            ObjectType.Group => Path.Combine(DeleteReplicationGlobalFileHandler.DeleteInfoDirectory, DeleteReplicationGlobalFileHandler.DeleteGroupReplicationFileName),
            ObjectType.OU => Path.Combine(DeleteReplicationGlobalFileHandler.DeleteInfoDirectory, DeleteReplicationGlobalFileHandler.DeleteOUReplicationFileName),
            _ => ""
        };
        var currReplicationTime = DateTime.Now.ToUniversalTime().ToString();
        var lastReplicationTime = await File.ReadAllTextAsync(filePath);
        await ProcessDeletedObjects(inputCreds, progress, objectType, cancellationToken, lastReplicationTime, recordsToSyncInSingleRequest);
        await File.WriteAllTextAsync(filePath, currReplicationTime, cancellationToken);
    }

    private static async Task ProcessDeletedObjects(InputCreds inputCreds, IProgress<Status>? progress, ObjectType objectType, CancellationToken cancellationToken, string? lastReplicationTime, int recordsToSyncInSingleRequest)
    {
        progress?.Report(new($"Processing Delete Replication {objectType}. {Environment.NewLine}", ""));

        var whenChangedFilter = string.IsNullOrEmpty(lastReplicationTime) ? "" : DateTime.Parse(lastReplicationTime).ToString("yyyyMMddHHmmss.0Z");
        using var root = await GetRootEntry(inputCreds, string.Empty);

        using var searcher = new DirectorySearcher(root);
        searcher.PropertiesToLoad.Add("objectguid");
        searcher.PropertiesToLoad.Add("distinguishedName");
        searcher.Tombstone = true;
        searcher.Filter = PrepareLdapQuery(objectType, whenChangedFilter, searchDeletedObjects: true);
        searcher.PageSize = 500;
        searcher.SizeLimit = 0;

        using var results = await Task.Run(() => searcher?.FindAll());
        var resultsEnumerator = results?.GetEnumerator();

        if (resultsEnumerator != null)
        {
            var objectGuidList = new List<(string, string)>();

            int i = 0;
            for (; resultsEnumerator.MoveNext(); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = (SearchResult)resultsEnumerator.Current;

                var objectGuid = Convert.ToBase64String((byte[])result.Properties["objectguid"][0]);
                var distinguishedName = result.Properties["distinguishedName"][0] as string ?? "";

                objectGuidList.Add((objectGuid, distinguishedName));


                if ((i + 1) % recordsToSyncInSingleRequest == 0)
                    ReportFetchDeletedObjects(objectType, objectGuidList, i + 1, progress);

                if ((i + 1) % recordsToSyncInSingleRequest == 0)
                    await SendObjectListToWebService(inputCreds.Host, inputCreds.DomainId, objectGuidList.Select(x => x.Item1).ToList(), objectType, progress, cancellationToken, "&action=delete");
            }
            if (objectGuidList.Count > 0)
                ReportFetchDeletedObjects(objectType, objectGuidList, i, progress);

            if (objectGuidList.Count > 0)
                await SendObjectListToWebService(inputCreds.Host, inputCreds.DomainId, objectGuidList.Select(x => x.Item1).ToList(), objectType, progress, cancellationToken, "&action=delete");


        }
    }
    #endregion

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
    private static async Task SendObjectListToWebService<T>(string host, string domainID, List<T> objectsList, ObjectType objectType, IProgress<Status>? progress, CancellationToken cancellationToken, string action = "")
    {
        progress?.Report(new("", SendingObjectsRequestMessage(objectsList.Count, objectType, action)));

        var json = await SerializerHelper.GetSerializedObject(objectsList);
        string apiUrl = objectType switch
        {
            ObjectType.User => $"{host}/active-directory/sync-data?domainId={domainID}&type=user{(string.IsNullOrEmpty(action) ? string.Empty : action)}",
            ObjectType.Group => $"{host}/active-directory/sync-data?domainId={domainID}&type=group{(string.IsNullOrEmpty(action) ? string.Empty : action)}",
            ObjectType.OU => $"{host}/active-directory/sync-data?domainId={domainID}&type=ou{(string.IsNullOrEmpty(action) ? string.Empty : action)}",
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
    private static string PrepareLdapQuery(ObjectType objectType, string whenChangedFilter, bool searchDeletedObjects = false)
    {
        string deletedObjects = $"{(searchDeletedObjects ? "(isDeleted=TRUE)" : string.Empty)}";
        string ldapfilter = objectType switch
        {
            ObjectType.User => string.IsNullOrEmpty(whenChangedFilter) ? $"(&{deletedObjects}(objectClass=user))" : $"(&{deletedObjects}(objectClass=user)(whenChanged>={whenChangedFilter}))",
            ObjectType.Group => string.IsNullOrEmpty(whenChangedFilter) ? $"(&{deletedObjects}(objectClass=group))" : $"(&{deletedObjects}(objectClass=group)(whenChanged>={whenChangedFilter}))",
            ObjectType.OU => string.IsNullOrEmpty(whenChangedFilter) ? $"(&{deletedObjects}(objectClass=organizationalUnit))" : $"(&{deletedObjects}(objectClass=organizationalUnit)(whenChanged>={whenChangedFilter}))",
            _ => ""
        };

        return ldapfilter;
    }

    private static string FetchObjectsMessage(ObjectType objectType, List<string> list)
    {
        var sb = new StringBuilder();
        list.ForEach(x => sb.Append($"fetch {objectType} {x}{Environment.NewLine}"));
        return sb.ToString();
    }

    private static string SendingObjectsRequestMessage(int count, ObjectType objectType, string action)
    {
        return $"Sending {(string.IsNullOrEmpty(action) ? action : "sync")} ${count} {objectType}s request.{Environment.NewLine}";
    }

    private static void ReportFetchObjects(ObjectType objectType, List<string> dnList, int i, IProgress<Status>? progress)
    {
        progress?.Report(new($"{FetchObjectsMessage(objectType, dnList)} id: {i} {Environment.NewLine}", ""));
        dnList.Clear();
    }

    private static void ReportFetchDeletedObjects(ObjectType objectType, List<(string, string)> objectGuid, int i, IProgress<Status>? progress)
    {
        var sb = new StringBuilder();
        objectGuid.ForEach(x => sb.Append($"fetch deleted {objectType} objectguid={x.Item1} , dn={x.Item2}{Environment.NewLine}"));
        progress?.Report(new($"{sb} id: {i} {Environment.NewLine}", ""));
    }

    #endregion


}
#pragma warning restore CA1416