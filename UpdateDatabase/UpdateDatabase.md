#UpdateDatabase
This small application has two modes of running.

1. Update mode
2. Upgrade mode

##Update mode
When a project-file (``.sqlproj``) is specified or the app is run from the same folder as a project-file, the following steps will be taken:

1. Load project-file
2. Increment minor version of ``DacVersion``-property.
3. Build the project
    - On fail: Reset old version
4. Find the newest ``.dacpac``-file
5. Copy it to the Snapshots-folder
6. Include it in the project-file.

###Examples
        UpdateDatabase.exe
or

        UpdateDatabase.exe "C:\Development\Project.Database\Project.Database.sqlproj"

##Upgrade mode
When a publish-file with or without extension (``.publish.xml``) is specified, the following steps will be taken:

1. Load project-file (``.sqlproj`` from same folder as the publish-file)
2. Find the latest snapshot (by sorted filenames)
3. Look up the target database version and compare to ``.dacpac``-file

Then, depending on the version comparison, one of the following tasks will be executed:

- Create database if the target doesn't exist
- Upgrade database if ``.dacpac``-version is newer than existing
- Do nothing if the version is the same

###Example
        UpdateDatabase.exe "C:\Development\Project.Database\DevSqlDeploy"