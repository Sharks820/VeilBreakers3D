# Tripo3D Bridge Troubleshooting Guide

## Problem: "Port is not working" / "Connection Failed"

The Tripo3D bridge requires a WebSocket connection on **port 60600**. If this isn't working, follow these steps:

---

## Step 1: Check if Blender is Running

⚠️ **The Tripo3D server only runs when Blender is open!**

1. Open Blender (version 4.1 or higher)
2. Check the **Tripo** panel in the sidebar (press `N` key)
3. Look for "Connection Status" - it should show connected or ready

---

## Step 2: Run Diagnostics in Blender

1. In Blender, switch to the **Scripting** workspace
2. Open the file: `Tools/DCC_Bridge/tripo_diagnose.py`
3. Click the **Run Script** button
4. Check the output in the System Console

**To open System Console:**
- Windows: Window → Toggle System Console
- Or check the terminal that launched Blender

---

## Step 3: Check if Port is Blocked

### Windows Firewall
The WebSocket server might be blocked by Windows Firewall:

1. Open Windows Security → Firewall & network protection
2. Click "Allow an app through firewall"
3. Find Blender in the list
4. Make sure both "Private" and "Public" are checked
5. If not listed, add Blender manually

### Check Port Usage
Open Command Prompt (as Admin) and run:
```cmd
netstat -ano | findstr 60600
```

If you see output, another program is using the port.

---

## Step 4: Manual Server Start

If the addon is installed but server won't start:

1. In Blender, go to **Scripting** tab
2. Create a new text file
3. Paste this code:

```python
import bpy
from Tripo3d_Blender_Bridge.core.ws_server import Server
import threading

# Create and start server
server = Server(port=60600)
thread = threading.Thread(target=server.run, daemon=True)
thread.start()

print("🚀 Server started on port 60600!")
```

4. Click **Run Script**
5. Check System Console for errors

---

## Step 5: Reinstall the Addon

If nothing else works:

### Remove Old Installation:
1. In Blender: Edit → Preferences → Add-ons
2. Find "Tripo Bridge"
3. Click **Remove**
4. Restart Blender

### Reinstall:
1. In Unity: VeilBreakers → Tripo3D Bridge
2. Click **"Install Tripo Addon"**
3. Wait for "Installation successful" message
4. Restart Blender

---

## Common Error Messages

### "Port 60600 is already in use"
**Cause:** Another instance of Blender (or another program) is using the port.

**Fix:**
1. Close all Blender instances
2. Open Task Manager → find "blender.exe"
3. End any remaining Blender processes
4. Reopen Blender

### "ModuleNotFoundError: No module named 'Tripo3d_Blender_Bridge'"
**Cause:** The addon isn't installed or isn't named correctly.

**Fix:**
1. Check the addon folder name is exactly `Tripo3d_Blender_Bridge`
2. Reinstall using the Unity menu or batch file

### "Error in event loop" or "Address already in use"
**Cause:** The WebSocket library is having issues.

**Fix:**
1. Restart Blender
2. If persists, try changing the port (advanced users only)

### "Websocket connection failed" in browser
**Cause:** Tripo Studio can't reach the Blender server.

**Fix:**
1. Ensure Blender is running
2. Check Windows Firewall isn't blocking
3. Try refreshing Tripo Studio page
4. Check that you're on the same machine (localhost)

---

## Alternative: Use FBX Export Instead

If you can't get the live connection working:

1. Generate model in Tripo Studio
2. Download as FBX
3. Save to: `Assets/Resources/Art/3D_Models/Tripo/`
4. Unity will auto-import with optimized settings

---

## Still Not Working?

Check these:

1. **Blender Version**: Must be 4.1 or higher
   - Check: Help → About Blender
   
2. **Python Version**: Should be 3.11+ (comes with Blender 4.1+)
   - In Blender Scripting tab: `import sys; print(sys.version)`

3. **Antivirus**: Some antivirus software blocks WebSocket servers
   - Temporarily disable to test
   - Add Blender as an exception

4. **VPN/Proxy**: May interfere with localhost connections
   - Temporarily disable to test

---

## Need More Help?

- Check the System Console in Blender (Window → Toggle System Console)
- Look for red error messages
- Copy the full error and search online
- Tripo3D support: https://www.tripo3d.ai/
