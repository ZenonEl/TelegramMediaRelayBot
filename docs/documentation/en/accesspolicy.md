# 🔐 Access Policy

**Access Policy** is a feature that allows you to control how the bot interacts with new users. This is especially useful if you want to restrict access to the bot and keep it only for your acquaintances.

---

### 🛠 Configuring Access Policy

To configure the access policy, open the `appsettings.json` file and find the `AccessPolicy` section. Here is an example configuration:

```json
"AccessPolicy": {
  "Enabled": true,
  "NewUsersPolicy": {
    "Enabled": true,
    "AllowNewUsers": true,
    "AllowRules": {
      "AllowAll": false,
      "WhitelistedReferrerIds": [],
      "BlacklistedReferrerIds": []
    }
  }
}
```

---

### 🎯 Key Parameters

#### 1. **Enabling/Disabling Access Policy**
- **Enabled**: Enables or disables the access policy. If `false`, all users will have access to the bot.

#### 2. **Policy for New Users**
- **NewUsersPolicy.Enabled**: Enables or disables the policy for new users.
- **AllowNewUsers**: Allows or denies access to new users.
  - If `true`, new users can use the bot.
  - If `false`, access will be restricted to existing users only.

#### 3. **Access Rules**
- **AllowAll**: If `true`, all users gain access to the bot.
- **WhitelistedReferrerIds**: A list of Telegram IDs of users who can always add new users.
   - For example:
     ```json
     "WhitelistedReferrerIds": [123456789, 987654321]
     ```
- **BlacklistedReferrerIds**: A list of Telegram IDs of users who will never be able to add new users.
   - For example:
     ```json
     "BlacklistedReferrerIds": [111111111, 222222222]
     ```

---

### ⚠️ Important Notes

- **User IDs**: Make sure you are using the correct Telegram user IDs.
- **Testing**: After changing the settings, test the bot to ensure the access policy works as expected.

---

### 💡 Tips

- If you want to fully open access to the bot, set `"AllowAll": true`. Or simply set `"NewUsersPolicy.Enabled": false"`.
- For stricter control, use a Whitelist to ensure only trusted users can add new members.