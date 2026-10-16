# Security notes

- Passwords are stored using ASP.NET Core's password hashing API, not plaintext storage.
- JWT tokens include only the user ID, email, and role claims needed for authorization.
- Role selection is restricted to the two approved values: `Organiser` and `Participant`.
