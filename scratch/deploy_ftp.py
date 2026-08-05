import os
import ftplib
import ssl

host = 'WIN8236.site4now.net'
user = 'tspmasterprd'
passwd = 'ftp@dm1n1str@t0r'
local_publish_dir = r'C:\GitHub\TSPMaster\publish'

def upload_dir(ftp, local_dir, remote_dir):
    print(f"Syncing folder: {remote_dir}")
    try:
        ftp.cwd(remote_dir)
    except Exception:
        try:
            ftp.mkd(remote_dir)
            ftp.cwd(remote_dir)
        except Exception as e:
            print(f"Error creating/changing remote dir {remote_dir}: {e}")
            return

    for item in os.listdir(local_dir):
        local_path = os.path.join(local_dir, item)
        if os.path.isfile(local_path):
            print(f"  Uploading file: {item}")
            with open(local_path, 'rb') as f:
                ftp.storbinary(f"STOR {item}", f)
        elif os.path.isdir(local_path):
            upload_dir(ftp, local_path, f"{remote_dir}/{item}")
            ftp.cwd("..")

print(f"Connecting to FTP server {host} via Explicit TLS...")
try:
    context = ssl.create_default_context()
    context.check_hostname = False
    context.verify_mode = ssl.CERT_NONE

    ftp = ftplib.FTP_TLS(context=context)
    ftp.connect(host, 21, timeout=15)
    ftp.login(user, passwd)
    ftp.prot_p()  # Switch to secure data connection
    print("FTP_TLS (Explicit) login successful! Starting publish upload...")

    # Upload files to root / site folder
    upload_dir(ftp, local_publish_dir, "/")
    ftp.quit()
    print("FTP publish completed successfully!")
except Exception as e:
    print(f"FTP Upload Failed: {e}")
