Windows Service + MSI Installer C#
Vantage Service that sync AD objects and upload to Vantage Server with the specified interval.

devBranch-> Latest Code

release#1-> Sync Setting Daily/Weekly Frequency, Sync Now, Sync All, Logs rotation, Upload Log File, Setting Getting Interval along with Setup installation

https://github.com/user-attachments/assets/73860515-05bc-4a03-97ab-10c8c37513a0

Compile And Run Instructions:

Step#1 install wixv3.11.2 toolset
https://github.com/wixtoolset/wix3/releases/tag/wix3112rtm
![Step#1 install wix toolset](https://github.com/user-attachments/assets/00d68aa0-2cc4-4de9-b800-76ebcf228bc8)


Step#2 make sure to install 2 visual studio extensions
![Step#2 install extensions](https://github.com/user-attachments/assets/e29b95db-cebc-4a47-aac6-7755940e9490)


Step#3 clone this repository and open the solution

Step#4 make sure first to publish VantageConnectorService
![Step#3](https://github.com/user-attachments/assets/247273a8-7405-431b-a107-4ac84c4fbf3e)


Step#5 build the VantageConnectorSetup

![Step#5 build installer setup](https://github.com/user-attachments/assets/50bfc7d8-1c8a-4d21-952c-54a5b84e2a19)


Step#6 Copy the setup from the folder C:\YourUser\ProjectFolder\VantageConnector\VantageConnectorService\SetupProject4\bin\Debug
![Step#6 setup](https://github.com/user-attachments/assets/aabad269-fc1c-4d38-8732-688e154884d1)
