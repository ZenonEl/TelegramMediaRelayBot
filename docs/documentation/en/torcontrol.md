# 🧅 Tor Control Management

The bot supports Tor integration, which is especially useful if you want to maintain privacy or bypass blocking on certain platforms.
**Tor Control** is a feature that allows you to automatically change chains within the Tor network to provide greater anonymity and bypass platform restrictions when downloading media files.

---

### 🛠 Configuring Tor

To enable and configure Tor, follow these steps:

#### 1. **Creating a Hashed Password**
To create a hashed password (`HashedControlPassword`), follow these steps:

1. **Install Tor** if it’s not already installed:
   ```bash
   sudo apt update && sudo apt install tor
   # Or for Arch Linux:
   sudo pacman -S tor
   ```

2. **Generate a Hashed Password**:
   Use the `tor --hash-password` command to generate a hashed password. For example:
   ```bash
   tor --hash-password "your_password"
   ```
   This command will return a string like:
   ```
   16:660537E3E1CD49996044A3BF558097A981F539FEA2F9DA662B4626C1C2
   ```
   This is your hashed password, which you need to add to `torrc`.

3. **Add the Hashed Password to `torrc`**:
   Open the Tor configuration file:
   ```bash
   sudo nano /etc/tor/torrc
   ```
   Add the following lines:
   ```
   ControlPort 9051
   HashedControlPassword 16:660537E3E1CD49996044A3BF558097A981F539FEA2F9DA662B4626C1C2
   ```
   Here, `16:660537E3E1CD49996044A3BF558097A981F539FEA2F9DA662B4626C1C2` is an example of the hashed password you generated in the previous step.

4. **Save the File and Restart Tor**:
   After making changes, save the file and exit the editor. Then restart Tor:
   ```bash
   sudo systemctl restart tor
   ```
> [!TIP]
> If the changes don’t take effect, try the following:
>  ```bash
>  sudo systemctl stop tor
>  sudo systemctl start tor
>  ```

---

#### 2. **Important Notes**
- **Security**: Ensure your password is strong.
- **Restart Tor**: Always restart Tor after modifying `torrc` to apply the changes.

> [!TIP]
> - Always use the `tor --hash-password` command to generate a hashed password.
> - Ensure that ControlPort is only accessible locally or protected by a firewall to prevent unauthorized access.

---

#### 3. **Configure the Bot**:
   - In the `appsettings.json` file, add or modify the `Tor` section:
     ```json
     "Tor": {
       "Enabled": true,
       "TorControlPassword": "your_password",
       "TorSocksHost": "127.0.0.1",
       "TorSocksPort": 9050,
       "TorControlPort": 9051,
       "TorChangingChainInterval": 5
     }
     ```

---

### 🔄 IP Address Rotation

The bot supports automatic IP address rotation via Tor. This is useful if you want to avoid blocks or maintain anonymity.

- **TorChangingChainInterval**: Specifies the interval (in minutes) at which the bot will change its IP address. For example, a value of `5` means the IP will change every 5 minutes.

---

### ⚠️ Important Considerations

- **Performance**: Using Tor may slow down content downloads due to the nature of the network.
- **Security**: Ensure your Tor Control password is strong and stored securely.
- **Limitations**: Some platforms may block requests coming through Tor. In such cases, you can temporarily disable Tor.

---

### 💡 Tips

- If you don’t want to use Tor Control, set `"Enabled": false` in the bot’s configuration. You can still use Tor as a proxy via the "Proxy" parameter.
- To test Tor functionality, you can use the `curl` command through Tor:
  ```bash
  torsocks curl https://check.torproject.org
  ```
