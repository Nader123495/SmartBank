import urllib.request, http.cookiejar, json, sys

# ============================================================
# CONFIGURATION - Entrez votre mot de passe d'application Gmail
# ============================================================
GMAIL_USER     = 'ngattoussa2002@gmail.com'
GMAIL_APP_PWD  = 'mwcsuopfuxfmwltw'  # App Password Gmail
# ============================================================

N8N_URL = 'http://localhost:5678'
N8N_EMAIL = 'ngattoussa2002@gmail.com'
N8N_PASS  = 'SmartBank2026!'

# --- Login ---
cookie_jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))
login_data = json.dumps({'emailOrLdapLoginId': N8N_EMAIL, 'password': N8N_PASS}).encode()
opener.open(urllib.request.Request(f'{N8N_URL}/rest/login', data=login_data,
            headers={'Content-Type': 'application/json'}, method='POST'))
cookies_str = '; '.join([f'{c.name}={c.value}' for c in cookie_jar])
headers = {'Content-Type': 'application/json', 'Cookie': cookies_str}
print("[OK] Connecte a n8n")

# --- Créer le credential SMTP Gmail ---
smtp_payload = {
    'name': 'smartbank-smtp',
    'type': 'smtp',
    'data': {
        'host': 'smtp.gmail.com',
        'port': 465,
        'secure': True,
        'user': GMAIL_USER,
        'password': GMAIL_APP_PWD
    }
}
req = urllib.request.Request(
    f'{N8N_URL}/rest/credentials',
    data=json.dumps(smtp_payload).encode(),
    headers=headers, method='POST'
)
resp = json.loads(opener.open(req).read().decode())
new_cred = resp.get('data', {})
cred_id   = new_cred.get('id', '')
cred_name = new_cred.get('name', 'smartbank-smtp')
print(f"[OK] Credential SMTP cree: {cred_name} (ID: {cred_id})")

# --- Lier le credential au Workflow 1 ---
wf_list = json.loads(opener.open(urllib.request.Request(
    f'{N8N_URL}/rest/workflows', headers=headers)).read().decode()).get('data', [])

for wf_meta in wf_list:
    if 'Workflow 1' not in wf_meta['name'] and 'clamation' not in wf_meta['name']:
        continue
    wf = json.loads(opener.open(urllib.request.Request(
        f'{N8N_URL}/rest/workflows/{wf_meta["id"]}', headers=headers)).read().decode()).get('data', {})
    
    modified = False
    for node in wf['nodes']:
        if node['type'] == 'n8n-nodes-base.emailSend':
            node['credentials'] = {'smtp': {'id': cred_id, 'name': cred_name}}
            modified = True
    
    if modified:
        opener.open(urllib.request.Request(
            f'{N8N_URL}/rest/workflows/{wf["id"]}',
            data=json.dumps(wf).encode(), headers=headers, method='PUT'))
        print(f"[OK] Workflow '{wf['name']}' lie au credential SMTP !")

print("\n[DONE] SMTP configure ! Relancez votre workflow dans n8n.")
