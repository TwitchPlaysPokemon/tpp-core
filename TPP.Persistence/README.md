This project simply defines all [Models](Models) that can be persisted and their accompanying [Repository interfaces](Repos) to perform operations on,
without imposing how the persistence may be implemented.
This realizes the [Data Access Object pattern](https://en.wikipedia.org/wiki/Data_access_object).

Application code is advised to only depend on classes from this project
 and not any concrete implementation.
