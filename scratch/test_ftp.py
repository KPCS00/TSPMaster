import ftplib

host = 'WIN8236.site4now.net'
user = 'tspmasterprd'
passwd = 'tspmasterprd'

try:
    print("Connecting to FTP_TLS...")
    ftps = ftplib.FTP_TLS()
    ftps.connect(host, 21, timeout=10)
    print("Sending auth()...")
    ftps.auth()
    print("Sending PBSZ 0 & PROT P before login...")
    ftps.sendcmd('PBSZ 0')
    ftps.sendcmd('PROT P')
    print("Logging in with user/pass...")
    res = ftps.login(user, passwd)
    print(f"=================> SUCCESS! Response: {res}")
    ftps.retrlines('LIST')
    ftps.quit()
except Exception as e:
    print(f"FAILED: {e}")
