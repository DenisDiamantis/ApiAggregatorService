API Aggregator Service — README
Overview

The API Aggregator Service is an ASP.NET Core Web API that consolidates data from multiple external providers, including:

GitHub
NewsAPI
WeatherAPI

The service offers unified endpoints, caching, performance tracking, and JWT-based authentication.

The service uses JWT Bearer Authentication. To obtain a token, call:

POST /auth/login

Request Body
{
"username": "testuser",
"password": "password"
}

Response
{
"token": "<JWT_TOKEN_HERE>"
}

Authorization: Bearer <JWT_TOKEN_HERE>

GET /api/aggregate

Parameters
| Name          | Type   | Required | Description                                                                                                                       |
| ------------- | ------ | -------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `cityWeather` | string | Yes      | City name used to fetch weather data.                                                                                             |
| `category`    | enum   | Yes      | News category (e.g., `general`, `sports`, `business`).                                                                            |
| `githubUser`  | string | Yes      | GitHub username for repository lookup.                                                                                            |
| `sortBy`      | enum   | No       | Sorting mode for GitHub repos: `Alphabetical`, `LastUpdated`, `stars`.                                                            |
| `ascending`   | bool   | No       | Sort direction. Default: `false` (descending).                                                                                    |
| `limit`       | int    | No       | Maximum number of items returned **per section**.                                                                                 |


GET /api/statistics

Returns a dictionary of API names → stats object.


Caching

The service uses IApiCacheService, backed by :
In-memory cache
Fallback Behavior
If an external API call fails:
The service logs the failure.
It attempts to return cached data.
If no cache exists → returns empty data, not an error.

