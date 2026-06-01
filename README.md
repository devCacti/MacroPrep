## Project Server Secrets Configuration Defaults:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=Where\\ServerName;Database=DatabaseName;Trusted_Connection=Bool;TrustServerCertificate=Bool"
  },
  "JWT": {
    "Key": "A_64_Character_Long_Key_To_Be_Used_By_The_JWT_System",
    "Issuer": "Server",
    "Audience": "Client",
    "ExpiryMinutes": 60
  }
}
```
