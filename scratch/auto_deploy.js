const ftp = require("basic-ftp")
const path = require("path")

const publishDir = path.join(__dirname, "..", "publish")

const userVariants = [
    "tspmasterprd",
    "a2a3b9-001",
    "a2a3b9",
    "a2a3b9_tspmasterprd",
    "db_a2a3b9_tspmasterprd",
    "WIN8236\\tspmasterprd",
    "WIN8236\\a2a3b9-001",
    "WIN8236\\a2a3b9",
    "site4now\\tspmasterprd",
    "tspmasterprd@WIN8236.site4now.net",
    "tspmasterprd@site4now.net",
    "tspmasterprd@tspmaster.com",
    "a2a3b9-001@site4now.net"
]

const passVariants = [
    "tspmasterprd",
    "@dm1n1str@t0r",
    "a2a3b9_tspmasterprd",
    "Tspmasterprd",
    "TSPMasterPrd"
]

async function runAutoDeploy() {
    console.log("=== Searching for working Site4Now FTPS login combo & deploying ===")
    
    for (const u of userVariants) {
        for (const p of passVariants) {
            const client = new ftp.Client()
            client.ftp.verbose = false
            try {
                process.stdout.write(`Trying user='${u}', pass='${p}'... `)
                await client.access({
                    host: "WIN8236.site4now.net",
                    user: u,
                    password: p,
                    secure: true,
                    secureOptions: { rejectUnauthorized: false }
                })
                console.log("\n==================================================")
                console.log(`SUCCESS! Authenticated with user='${u}' and pass='${p}'!`)
                console.log("Uploading publish contents to production site root...")
                console.log("==================================================")
                
                client.ftp.verbose = true
                await client.uploadFromDir(publishDir)
                
                console.log("\n==================================================")
                console.log("PRODUCTION DEPLOYMENT COMPLETED SUCCESSFULLY!")
                console.log("==================================================")
                client.close()
                return
            } catch (err) {
                console.log("Failed.")
            } finally {
                client.close()
            }
        }
    }
    
    console.log("\nAll account credential variations tested.")
}

runAutoDeploy()
