const ftp = require("basic-ftp")

async function testFtp() {
    const client = new ftp.Client()
    client.ftp.verbose = true
    try {
        console.log("Connecting via basic-ftp with Explicit TLS (secure: true)...")
        await client.access({
            host: "WIN8236.site4now.net",
            user: "tspmasterprd",
            password: "tspmasterprd",
            secure: true,
            secureOptions: {
                rejectUnauthorized: false
            }
        })
        console.log("=================> SUCCESSFUL FTPS LOGIN VIA BASIC-FTP!")
        const list = await client.list()
        console.log("Directory listing:", list)
    } catch (err) {
        console.log("FTPS Error:", err)
    }
    client.close()
}

testFtp()
