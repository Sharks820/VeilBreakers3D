"""
Tripo3D Server Quick Start
==========================
Run this in Blender's Scripting tab to manually start the WebSocket server
if it's not starting automatically.

Instructions:
1. Open Blender
2. Switch to "Scripting" workspace (top tabs)
3. Click "Open" and select this file, OR paste the code below
4. Click "Run Script"
5. Check the System Console for "Server started on port 60600"

To open System Console: Window → Toggle System Console
"""

import bpy
import sys
import threading
import socket
import time

print("\n" + "="*60)
print("🚀 Starting Tripo3D WebSocket Server")
print("="*60)

# Check if already running
def is_port_in_use(port):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        return s.connect_ex(('127.0.0.1', port)) == 0

if is_port_in_use(60600):
    print("✅ Server is already running on port 60600!")
    print("📋 You can now use Tripo Studio to send models to Blender")
else:
    print("🔄 Starting server on port 60600...")
    
    try:
        # Import the server module
        from Tripo3d_Blender_Bridge.core.ws_server import Server
        from Tripo3d_Blender_Bridge.core.status_manager import status
        
        # Create server instance
        server = Server(port=60600)
        
        # Start in background thread
        server_thread = threading.Thread(target=server.run, daemon=True)
        server_thread.start()
        
        # Wait a moment for server to start
        time.sleep(2)
        
        # Verify it's running
        if is_port_in_use(60600):
            print("✅ SUCCESS! Server is running on port 60600")
            print("📌 Server thread: " + ("Alive" if server_thread.is_alive() else "Dead"))
            print("\n📝 Next steps:")
            print("   1. Open Tripo Studio: https://studio.tripo3d.ai/")
            print("   2. Generate a 3D model")
            print("   3. Click 'Send to Blender'")
            print("   4. Model will appear in your Blender scene!")
        else:
            print("❌ Server failed to start")
            print("   Check System Console for error details")
            
    except ImportError as e:
        print(f"❌ Error: Cannot import Tripo3D modules")
        print(f"   Details: {e}")
        print("\n💡 Solution:")
        print("   The addon might not be installed correctly.")
        print("   Reinstall via Unity: VeilBreakers → Tripo3D Bridge")
        
    except Exception as e:
        print(f"❌ Error starting server: {e}")
        import traceback
        traceback.print_exc()
        print("\n💡 Try:")
        print("   - Restart Blender")
        print("   - Check if another program uses port 60600")
        print("   - See TROUBLESHOOTING.md for more help")

print("="*60 + "\n")
