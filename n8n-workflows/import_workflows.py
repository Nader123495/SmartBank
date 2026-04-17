import urllib.request
import http.cookiejar
import json
import os

BASE_DIR = r"C:\Users\msi\Downloads\SmartBank\n8n-workflows"
N8N_URL = "http://localhost:5678"
EMAIL = "ngattoussa2002@gmail.com"
PASSWORD = "SmartBank2026!"

# Créer session avec cookies
cookie_jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))

# --- ÉTAPE 1 : Login ---
print("Connexion à n8n...")
login_data = json.dumps({
    "emailOrLdapLoginId": EMAIL,
    "password": PASSWORD
}).encode("utf-8")

try:
    resp = opener.open(urllib.request.Request(
        f"{N8N_URL}/rest/login",
        data=login_data,
        headers={"Content-Type": "application/json"}
    ))
    user = json.loads(resp.read().decode()).get("data", {})
    print(f"  Connecté en tant que : {user.get('firstName')} ({user.get('email')})")
except urllib.error.HTTPError as e:
    print(f"  ERREUR login: {e.code} - {e.read().decode()[:200]}")
    exit(1)

cookies_str = "; ".join([f"{c.name}={c.value}" for c in cookie_jar])

# --- ÉTAPE 2 : Lister les workflows existants ---
resp_list = opener.open(urllib.request.Request(
    f"{N8N_URL}/rest/workflows",
    headers={"Cookie": cookies_str}
))
existing = json.loads(resp_list.read().decode()).get("data", [])
existing_names = [wf["name"] for wf in existing]
print(f"\nWorkflows existants ({len(existing)}):")
for wf in existing:
    print(f"  - {wf['name']}")

# --- ÉTAPE 3 : Importer les workflows manquants ---
files_to_import = [
    "workflow2_sla_escalade.json",
    "workflow3_statut_notifications.json"
]

print("\nImportation des workflows...")
for filename in files_to_import:
    filepath = os.path.join(BASE_DIR, filename)
    with open(filepath, "r", encoding="utf-8") as f:
        wf_data = json.load(f)

    wf_name = wf_data.get("name", filename)

    if wf_name in existing_names:
        print(f"  DEJA PRESENT: {wf_name}")
        continue

    # Supprimer l'ID pour éviter les conflits
    wf_data.pop("id", None)
    wf_data["active"] = False  # Inactive jusqu'à configuration des credentials

    try:
        req = urllib.request.Request(
            f"{N8N_URL}/rest/workflows",
            data=json.dumps(wf_data).encode("utf-8"),
            headers={
                "Content-Type": "application/json",
                "Cookie": cookies_str
            },
            method="POST"
        )
        resp = opener.open(req)
        result = json.loads(resp.read().decode()).get("data", {})
        print(f"  IMPORTE: {result.get('name')} (ID: {result.get('id')})")
    except urllib.error.HTTPError as e:
        error_body = e.read().decode()
        print(f"  ERREUR import {filename}: {e.code} - {error_body[:300]}")

# --- ÉTAPE 4 : Vérification finale ---
resp_final = opener.open(urllib.request.Request(
    f"{N8N_URL}/rest/workflows",
    headers={"Cookie": cookies_str}
))
final_list = json.loads(resp_final.read().decode()).get("data", [])

print(f"\n{'='*50}")
print(f"LISTE FINALE ({len(final_list)} workflows dans n8n):")
print(f"{'='*50}")
for wf in final_list:
    status = "ACTIF" if wf.get("active") else "inactif"
    print(f"  [{status}] {wf['name']}")

print("\nOuvre http://localhost:5678 pour voir tes workflows SmartBank!")
