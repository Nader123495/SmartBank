import urllib.request, http.cookiejar, json, sys

N8N_URL = 'http://localhost:5678'
WF_FILE = 'workflow2_sla_escalade.json'

def update_n8n():
    cookie_jar = http.cookiejar.CookieJar()
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))
    
    # 1. Login
    login_data = json.dumps({'emailOrLdapLoginId': 'ngattoussa2002@gmail.com', 'password': 'SmartBank2026!'}).encode()
    try:
        opener.open(urllib.request.Request(f'{N8N_URL}/rest/login', data=login_data, headers={'Content-Type': 'application/json'}, method='POST'))
    except Exception as e:
        print(f"Login failed: {e}")
        return

    cookies_str = '; '.join([f'{c.name}={c.value}' for c in cookie_jar])
    headers = {'Content-Type': 'application/json', 'Cookie': cookies_str}

    # 2. Read local file
    try:
        with open(WF_FILE, 'r', encoding='utf-8') as f:
            local_wf = json.load(f)
    except Exception as e:
        print(f"Could not read {WF_FILE}: {e}")
        return

    # 3. Get all workflows
    try:
        req = urllib.request.Request(f'{N8N_URL}/rest/workflows', headers=headers)
        resp = opener.open(req).read().decode()
        wf_list = json.loads(resp).get('data', [])
    except Exception as e:
        print(f"Could not fetch workflows: {e}")
        return

    # 4. Find target workflow (by name or placeholder ID)
    target_wf = next((w for w in wf_list if "Workflow 2" in w.get('name', '')), None)
    
    if not target_wf:
        print("Workflow 'Workflow 2' not found in n8n. Trying to create it...")
        try:
            req_post = urllib.request.Request(f'{N8N_URL}/rest/workflows', data=json.dumps(local_wf).encode(), headers=headers, method='POST')
            opener.open(req_post)
            print("Successfully CREATED the workflow in n8n!")
            return
        except Exception as e:
            print(f"Failed to create workflow: {e}")
            return

    # 5. Update existing workflow
    wf_id = target_wf['id']
    try:
        # Get full object first
        req_get = urllib.request.Request(f'{N8N_URL}/rest/workflows/{wf_id}', headers=headers)
        db_wf = json.loads(opener.open(req_get).read().decode()).get('data', {})
        
        # Merge nodes and connections
        db_wf['nodes'] = local_wf['nodes']
        db_wf['connections'] = local_wf['connections']
        db_wf['name'] = local_wf['name']
        
        req_put = urllib.request.Request(f'{N8N_URL}/rest/workflows/{wf_id}', data=json.dumps(db_wf).encode(), headers=headers, method='PUT')
        opener.open(req_put)
        print(f"Successfully UPDATED workflow '{db_wf['name']}' (ID: {wf_id})")
    except Exception as e:
        print(f"Failed to update workflow: {e}")

if __name__ == "__main__":
    update_n8n()
