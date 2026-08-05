const ftp = require("basic-ftp")
const path = require("path")

async function deploy() {
    const client = new ftp.Client()
    client.ftp.verbose = true
    const publishDir = path.join(__dirname, "..", "publish")

    console.log("=== TSP Master Production Deployment via Explicit FTPS ===")
    console.log("Source directory:", publishDir)
    console.log("Target server:", "WIN8236.site4now.net:21")
    console.log("FTP User:", "tspmasterprd")

    try {
        await client.access({
            host: "WIN8236.site4now.net",
            user: "tspmasterprd",
            password: "ftp@dm1n1str@t0r",
            secure: true,
            secureOptions: {
                rejectUnauthorized: false
            }
        })
        console.log("==================================================")
        console.log("FTPS connection and authentication SUCCESSFUL!")
        console.log("Uploading release files to production site root...")
        console.log("==================================================")
        
        await client.uploadFromDir(publishDir)
        console.log("==================================================")
        console.log("SUCCESS! TSP Master published to production over FTPS.")
        console.log("==================================================")
    } catch (err) {
        console.error("Deploy failed:", err.message || err)
    } finally {
        client.close()
    }
}

deploy()
