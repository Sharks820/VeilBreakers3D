#!/usr/bin/env python3
"""
Tripo3D Bridge Diagnostic Script
Run this in Blender's Scripting tab to diagnose connection issues
"""

import bpy
import sys
import os

print("=" * 60)
print("Tripo3D Bridge Diagnostics")
print("=" * 60)

# Check 1: Is the addon enabled?
print("\n[1] Checking if Tripo3D addon is enabled...")
is_enabled = False
addon_name = "Tripo3d_Blender_Bridge"

for mod in bpy.context.preferences.addons:
    if addon_name in mod.module:
        is_enabled = True
        print(f"   ✓ Addon found: {mod.module}")
        break

if not is_enabled:
    print(f"   ✗ Addon '{addon_name}' is NOT enabled!")
    print("   → Go to Edit → Preferences → Add-ons and enable it")
else:
    print(f"   ✓ Addon is enabled")

# Check 2: Try to import the addon modules
print("\n[2] Checking addon imports...")
try:
    from Tripo3d_Blender_Bridge.core.ws_server import Server
    print("   ✓ WebSocket Server module found")
except Exception as e:
    print(f"   ✗ Cannot import WebSocket Server: {e}")

try:
    from Tripo3d_Blender_Bridge.core.status_manager import status
    print("   ✓ Status manager found")
    print(f"   Current status: Connected={status.is_connected}, Port={status.port if hasattr(status, 'port') else 'N/A'}")
except Exception as e:
    print(f"   ✗ Cannot import status manager: {e}")

# Check 3: Check if server can be started
print("\n[3] Checking WebSocket server...")
try:
    import socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    result = sock.connect_ex(('127.0.0.1', 60600))
    if result == 0:
        print("   ✓ Port 60600 is already in use (server may be running)")
    else:
        print(f"   ✗ Port 60600 is NOT in use (result: {result})")
        print("   → The server needs to be started")
    sock.close()
except Exception as e:
    print(f"   ✗ Error checking port: {e}")

# Check 4: Try to start the server manually
print("\n[4] Attempting to start server...")
try:
    # Try to find and start the server
    from Tripo3d_Blender_Bridge.core.ws_server import Server
    import threading
    
    server = Server(port=60600)
    
    # Check if server thread already exists
    server_thread = threading.Thread(target=server.run, daemon=True)
    server_thread.start()
    
    print("   ✓ Server thread started")
    print("   Waiting 2 seconds for server to initialize...")
    
    import time
    time.sleep(2)
    
    # Check again
    import socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    result = sock.connect_ex(('127.0.0.1', 60600))
    if result == 0:
        print("   ✓ Server is now running on port 60600!")
    else:
        print(f"   ✗ Server failed to start (result: {result})")
    sock.close()
    
except Exception as e:
    print(f"   ✗ Error starting server: {e}")
    import traceback
    traceback.print_exc()

# Check 5: Check Python version
print("\n[5] Python/Blender info...")
print(f"   Python version: {sys.version}")
print(f"   Blender version: {bpy.app.version_string}")

print("\n" + "=" * 60)
print("Diagnostics complete!")
print("=" * 60)

if not is_enabled:
    print("\n⚠️  ACTION NEEDED: Enable the Tripo3D addon in preferences")
else:
    print("\n✅ Addon is enabled. If server still won't start:")
    print("   1. Check Blender's System Console for errors")
    print("   2. Try reinstalling the addon")
    print("   3. Check if another app is using port 60600")
