import urllib.request, http.cookiejar, json

N8N_URL = 'http://localhost:5678'
WF_ID = 'UvPqgIL1FXTILX3h' # Récupéré depuis l'URL du navigateur de l'utilisateur
WF_FILE = 'n8n-workflows/workflow2_sla_escalade.json'

def force_update():
    cookie_jar = http.cookiejar.CookieJar()
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))
    
    # Login
    login_data = json.dumps({'emailOrLdapLoginId': 'ngattoussa2002@gmail.com', 'password': 'SmartBank2026!'}).encode()
    opener.open(urllib.request.Request(f'{N8N_URL}/rest/login', data=login_data, headers={'Content-Type': 'application/json'}, method='POST'))
    
    cookies_str = '; '.join([f'{c.name}={c.value}' for c in cookie_jar])
    headers = {'Content-Type': 'application/json', 'Cookie': cookies_str}

    # Charger le JSON local
    with open(WF_FILE, 'r', encoding='utf-8') as f:
        local_wf = json.load(f)

    # Récupérer l'état actuel sur n8n pour ne pas perdre les métadonnées
    req_get = urllib.request.Request(f'{N8N_URL}/rest/workflows/{WF_ID}', headers=headers)
    db_wf = json.loads(opener.open(req_get).read().decode()).get('data', {})

    # Injecter les nouveaux nœuds et connections
    db_wf['nodes'] = local_wf['nodes']
    db_wf['connections'] = local_wf['connections']
    db_wf['name'] = local_wf['name']
    db_wf['active'] = True

    # Pousser la mise à jour
    req_put = urllib.request.Request(f'{N8N_URL}/rest/workflows/{WF_ID}', data=json.dumps(db_wf).encode(), headers=headers, method='PUT')
    opener.open(req_put)
    print(f"Workflow {WF_ID} mis à jour automatiquement avec succès !")

if __name__ == "__main__":
    force_update()
