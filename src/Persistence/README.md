This project simply defines repository interfaces for all [Models](../Model) that can be persisted,
without imposing how the persistence may be implemented.
This realizes the [Data Access Object pattern](https://en.wikipedia.org/wiki/Data_access_object).

Application code is advised to only depend on classes from this project
 and not any concrete implementation.
