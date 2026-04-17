import urllib.request, http.cookiejar, json

N8N_URL = 'http://localhost:5678'
cookie_jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))
login_data = json.dumps({'emailOrLdapLoginId': 'ngattoussa2002@gmail.com', 'password': 'SmartBank2026!'}).encode()
opener.open(urllib.request.Request(f'{N8N_URL}/rest/login', data=login_data, headers={'Content-Type': 'application/json'}, method='POST'))
cookies_str = '; '.join([f'{c.name}={c.value}' for c in cookie_jar])
headers = {'Content-Type': 'application/json', 'Cookie': cookies_str}

# 1. Credentials
req_cred = urllib.request.Request(f'{N8N_URL}/rest/credentials', headers=headers)
creds = json.loads(opener.open(req_cred).read().decode()).get('data', [])
sql_cred = next((c for c in creds if c['type'] == 'microsoftSql'), None)

if not sql_cred:
    cred_payload = {
        'name': 'SmartBank SQL Server',
        'type': 'microsoftSql',
        'nodesAccess': [{'nodeType': 'n8n-nodes-base.microsoftSql'}],
        'data': {
            'server': 'localhost',
            'port': 1433,
            'database': 'SmartBankDB',
            'user': 'sa',
            'password': 'SmartBank2026!',
            'domain': ''
        }
    }
    req_new_cred = urllib.request.Request(f'{N8N_URL}/rest/credentials', data=json.dumps(cred_payload).encode(), headers=headers, method='POST')
    sql_cred = json.loads(opener.open(req_new_cred).read().decode()).get('data', {})

cred_id = sql_cred['id']
cred_name = sql_cred['name']

# 2. Add credentials and Activate all 3 SmartBank workflows
wf_list = json.loads(opener.open(urllib.request.Request(f'{N8N_URL}/rest/workflows', headers=headers)).read().decode()).get('data', [])
target_workflows = ['Workflow 1', 'Workflow 2', 'Workflow 3']

for target in target_workflows:
    wf_summary = next((w for w in wf_list if target in w['name']), None)
    if not wf_summary:
        print(f"Workflow '{target}' not found")
        continue

    # Fetch full workflow details
    resp = json.loads(opener.open(urllib.request.Request(f'{N8N_URL}/rest/workflows/{wf_summary["id"]}', headers=headers)).read().decode())
    wf_full = resp.get('data', resp) if isinstance(resp, dict) else resp
    
    # Update SQL credentials for all SQL nodes
    for node in wf_full.get('nodes', []):
        if node['type'] == 'n8n-nodes-base.microsoftSql':
            node['credentials'] = {'microsoftSql': {'id': cred_id, 'name': cred_name}}
    
    # Activate workflow
    wf_full['active'] = True
    update_req = urllib.request.Request(f'{N8N_URL}/rest/workflows/{wf_full["id"]}', data=json.dumps(wf_full).encode(), headers=headers, method='PUT')
    opener.open(update_req)
    print(f"SUCCESS: {target} activated")
