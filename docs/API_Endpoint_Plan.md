# RaceDay API Endpoint Plan

**Module:** PROG6212 – Part 1, Section B
**System:** RaceDay – event management platform for the SA road running, walking and cycling community
**Roles:** `Organiser` (creates/manages events, categories, results), `Participant` (browses, enters events, tracks own results)

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. | None (public) | { fullName, email, password, role } | 201 Created – new user object (no password returned). 409 Conflict – email already registered. |
| POST | /api/auth/login | Authenticates a user and returns an access token. | None (public) | { email, password } | 200 OK – { token, userId, role }. 401 Unauthorized – invalid credentials. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any (logged in) | None | 200 OK – user profile. 401 Unauthorized. |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any (logged in) | { fullName, email } | 200 OK – updated profile. 400 Bad Request – invalid data. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all events so Participants can browse upcoming events. | Any | None | 200 OK – array of events. |
| GET | /api/events/{id} | Returns full details for a single event, including route info. | Any | None | 200 OK – event object. 404 Not Found. |
| POST | /api/events | Creates a new event. | Organiser | { eventName, description, eventDate, location, routeInfo, eventDistanceKm, eventType } | 201 Created – event object. 400 Bad Request. |
| PUT | /api/events/{id} | Updates an event the Organiser owns. | Organiser (owner) | { eventName, description, eventDate, location, routeInfo, eventDistanceKm, eventType } | 200 OK – updated event. 403 Forbidden. 404 Not Found. |
| DELETE | /api/events/{id} | Deletes an event the Organiser owns. | Organiser (owner) | None | 204 No Content. 403 Forbidden. 404 Not Found. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{id}/categories | Lists the categories available for a specific event (e.g. 5km, 10km). | Any | None | 200 OK – array of categories. 404 Not Found. |
| POST | /api/events/{id}/categories | Adds a new category to an event the Organiser owns. | Organiser (owner) | { categoryName, distanceKm, maxParticipants } | 201 Created – category object. 403 Forbidden. |
| PUT | /api/categories/{id} | Updates an existing category. | Organiser (owner) | { categoryName, distanceKm, maxParticipants } | 200 OK – updated category. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser (owner) | None | 204 No Content. 403 Forbidden. 404 Not Found. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{id}/enrolments | Enters the logged-in Participant into an event by enrolling in a category. | Participant | None | 201 Created – enrolment object. 409 Conflict – already enrolled. 400 Bad Request – category full. |
| GET | /api/users/me/enrolments | Returns the logged-in Participant's own enrolments. | Participant | None | 200 OK – array of enrolments. |
| GET | /api/events/{id}/enrolments | Returns all enrolments for an event the Organiser owns. | Organiser (owner) | None | 200 OK – array of enrolments. 403 Forbidden. |
| DELETE | /api/enrolments/{id} | Cancels the logged-in Participant's own enrolment. | Participant (owner) | None | 204 No Content. 403 Forbidden. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{id}/results | Captures a race result for a specific enrolment. | Organiser | { finishTime, position } | 201 Created – result object. 403 Forbidden. 409 Conflict – result already captured. |
| GET | /api/users/me/results | Returns the logged-in Participant's personal performance history. | Participant | None | 200 OK – array of results. |
| GET | /api/events/{id}/results | Returns the full results/leaderboard for an event. | Any | None | 200 OK – array of results. 404 Not Found. |
