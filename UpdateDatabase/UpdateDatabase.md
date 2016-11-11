#UpdateDatabase
This small application has two modes of running:

1. Update mode.
2. Upgrade mode.

##Update mode
The user is presented with options to decide the new version number. A snapshot is then created with that version.

###Examples
        UpdateDatabase.exe
or

        UpdateDatabase.exe "C:\Development\Project.Database\Project.Database.sqlproj"

##Upgrade mode

When a publish-file with or without extension (``.publish.xml``) is specified, the following steps will be taken:

1. Load project-file (``.sqlproj`` from same folder as the publish-file).
2. Find the latest snapshot (by sorted filenames).
3. Look up the target database version and compare to ``.dacpac``-file.

Then, depending on the version comparison, one of the following tasks will be executed:

- Create database if the target doesn't exist.
- Upgrade database if ``.dacpac``-version is newer than existing.
- Do nothing if the version is the same.

###Example
        ``UpdateDatabase.exe /p:"C:\Development\Project.Database\DevSqlDeploy.publish.xml"``

#Roadmap

#DAC Upgrader

#Deployment-mode

1. Find the latest version in History-folder.
2. Check the deployment target's version:
    - The target has the same version -> Stop.
    - The target does not exist -> Deploy latest version.
    - The target has an older version.
        1. Locate the older version in the upgrades folder and select all version since (excluding the current version and including the latest version).
        2. Sequentially deploy each version increment.