This project implements the [Persistence interface](../TPP.Persistence) using a MongoDB database.
It is designed to seamlessly function with the same database the old core uses.

## run MongoDB

- Install MongoDB 7.0 or higher. Get it [here](https://www.mongodb.com/try/download/community).
- Install MongoDB Shell, Get it [here](https://www.mongodb.com/try/download/shell).
- (optional) Install MongoDB Database Tools, Get them [here](https://www.mongodb.com/try/download/database-tools).
- The server must be in [replica set mode](https://docs.mongodb.com/manual/tutorial/convert-standalone-to-replica-set/).
  For a single instance, this can be achieved by adding this to the `mongod.cfg` file:
  ```
  replication:
    replSetName: rs0
  ```
  The config file is usually located at `/etc/mongod.conf` (linux) or `%PROGRAMFILES%/MongoDB/Server/<version>/bin/mongod.cfg` (windows)
- Restart MongoDB, and then run `rs.initiate()` from a mongo shell to initialize the replication set. You enter a mongo shell using `mongosh` (linux) or `%PROGRAMFILES%/MongoDB/Server/<version>/bin/mongosh.exe` (windows).
- The recommended replica set name is `rs0`, as that's a sensible default
  and you will then be able to execute the mongodb integration tests,
  if the server is running on localhost with the default port `27017`.
  If you already chose a different name, you can rename the replica set [like this](https://stackoverflow.com/a/33400608/3688648).
- If you run MongoDB with those defaults, running the [TPP.Core](../TPP.Core)
  project will also work without requiring any further configuration changes.
