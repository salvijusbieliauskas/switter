# switter

**switter** is an ASP .NET Core application built with **.NET 6.0** that facilitates the use of a single Twitter account by several users.

The project is no longer being maintained and likely will not function as intended with the current Twitter API.
## Features

- Publishing text and image posts to a single Twitter account
- Account system which allows users to create posts and receive credit for them
- A leaderboard which ranks users by how many likes their posts received

## Running the project
### Prerequisites

- .NET 6.0
- A Twitter account with API keys for automation
- MS SQL DB for data storage

### Build Instructions

1. Clone the repository:

   ```bash
   git clone https://github.com/salvijusbieliauskas/switter.git
   ```

2. Open the solution in a .NET IDE of your choice.

3. Restore any NuGet packages if required.

4. Assign values to the following configuration variables:
   - ConsumerKey
   - ConsumerSecret
   - AccessToken
   - BearerToken 
   - AccessSecret

5. Apply database migrations using:

   ```bash
   update-database
   ```

6. Build and run the project.

## License

[MIT License](switter/LICENSE)