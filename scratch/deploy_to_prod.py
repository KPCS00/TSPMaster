import os
import time
import ftplib
import io

FTP_HOST = "WIN8236.site4now.net"
FTP_USER = "tspmasterprd"
FTP_PASS = "ftp@dm1n1str@t0r"

def make_remote_dir(ftp, dir_path):
    dirs = dir_path.strip('/').split('/')
    current = ""
    for d in dirs:
        if not d: continue
        current += "/" + d
        try:
            ftp.mkd(current)
            print(f"Created remote directory: {current}")
        except Exception:
            pass # Directory likely exists

def upload_folder(ftp, local_folder, remote_base_path):
    local_folder = os.path.abspath(local_folder)
    make_remote_dir(ftp, remote_base_path)

    for root, dirs, files in os.walk(local_folder):
        rel_path = os.path.relpath(root, local_folder)
        if rel_path == ".":
            remote_dir = remote_base_path
        else:
            remote_dir = remote_base_path.rstrip("/") + "/" + rel_path.replace("\\", "/")

        make_remote_dir(ftp, remote_dir)

        for file_name in files:
            local_file = os.path.join(root, file_name)
            remote_file = remote_dir.rstrip("/") + "/" + file_name
            try:
                with open(local_file, "rb") as f:
                    ftp.storbinary(f"STOR {remote_file}", f)
                print(f"Uploaded: {remote_file}")
            except Exception as e:
                print(f"FAILED to upload {remote_file}: {e}")

def main():
    print(f"Connecting to FTP server {FTP_HOST}...")
    ftp = ftplib.FTP(FTP_HOST)
    ftp.login(FTP_USER, FTP_PASS)
    ftp.set_pasv(True)
    print("Logged in successfully.")

    # 1. Place app_offline.htm at root AND /api to stop IIS processes & unlock all DLLs
    print("\n=== Placing app_offline.htm at / and /api ===")
    app_offline_content = b"<html><body><h1>Updating TSP Master Production App...</h1></body></html>"
    try:
        ftp.storbinary("STOR /app_offline.htm", io.BytesIO(app_offline_content))
        ftp.storbinary("STOR /api/app_offline.htm", io.BytesIO(app_offline_content))
        print("Placed app_offline.htm at / and /api. Waiting 5 seconds for IIS process shutdown...")
        time.sleep(5)
    except Exception as e:
        print(f"Warning putting app_offline.htm: {e}")

    # 2. Deploy API to /api (Self-contained win-x64 build)
    print("\n=== Deploying Self-Contained API (win-x64) to /api ===")
    publish_api_dir = os.path.abspath("./publish_sc")
    if os.path.exists(publish_api_dir):
        upload_folder(ftp, publish_api_dir, "/api")
    else:
        print(f"Error: API publish folder {publish_api_dir} does not exist.")

    # 3. Deploy Client to /
    print("\n=== Deploying Client to / ===")
    publish_client_dir = os.path.abspath("./publish_client")
    if os.path.exists(publish_client_dir):
        upload_folder(ftp, publish_client_dir, "/")
    else:
        print(f"Error: Client publish folder {publish_client_dir} does not exist.")

    # 4. Remove app_offline.htm from / and /api to restart site
    print("\n=== Removing app_offline.htm to restart production IIS application ===")
    try:
        ftp.delete("/app_offline.htm")
        print("Removed /app_offline.htm.")
    except Exception as e:
        print(f"Warning removing /app_offline.htm: {e}")

    try:
        ftp.delete("/api/app_offline.htm")
        print("Removed /api/app_offline.htm.")
    except Exception as e:
        print(f"Warning removing /api/app_offline.htm: {e}")

    ftp.quit()
    print("\n=== Production Deployment Complete ===")

if __name__ == "__main__":
    main()
