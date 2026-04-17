import urllib.request
import http.cookiejar
import json
import os

BASE_DIR = r"C:\Users\msi\Downloads\SmartBank\n8n-workflows"
N8N_URL = "http://localhost:5678"
EMAIL = "ngattoussa2002@gmail.com"
PASSWORD = "SmartBank2026!"

# Créer session
cookie_jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))

login_data = json.dumps({"emailOrLdapLoginId": EMAIL, "password": PASSWORD}).encode("utf-8")
resp = opener.open(urllib.request.Request(f"{N8N_URL}/rest/login", data=login_data, headers={"Content-Type": "application/json"}))
cookies_str = "; ".join([f"{c.name}={c.value}" for c in cookie_jar])

# Get workflows
resp_list = opener.open(urllib.request.Request(f"{N8N_URL}/rest/workflows", headers={"Cookie": cookies_str}))
existing = json.loads(resp_list.read().decode()).get("data", [])

files_to_import = ["workflow1_nouvelle_reclamation.json"]

print("Re-importing workflows...")
for filename in files_to_import:
    filepath = os.path.join(BASE_DIR, filename)
    with open(filepath, "r", encoding="utf-8") as f:
        wf_data = json.load(f)

    wf_name = wf_data.get("name", filename)
    wf_data.pop("id", None)
    wf_data["active"] = False

    # Delete existing
    existing_wfs = [wf for wf in existing if wf["name"] == wf_name]
    for ewf in existing_wfs:
        try:
            req_del = urllib.request.Request(f"{N8N_URL}/rest/workflows/{ewf['id']}", headers={"Cookie": cookies_str}, method="DELETE")
            opener.open(req_del)
            print(f"  DELETED old '{wf_name}'")
        except Exception as e:
            print("  ERR DEL:", str(e))

    # Create new
    try:
        req = urllib.request.Request(
            f"{N8N_URL}/rest/workflows",
            data=json.dumps(wf_data).encode("utf-8"),
            headers={"Content-Type": "application/json", "Cookie": cookies_str},
            method="POST"
        )
        resp = opener.open(req)
        result = json.loads(resp.read().decode()).get("data", {})
        print(f"  IMPORTED: {result.get('name')} (ID: {result.get('id')})")
    except urllib.error.HTTPError as e:
        print(f"  ERR POST {filename}: {e.code} - {e.read().decode()[:300]}")

print("Termine!")
