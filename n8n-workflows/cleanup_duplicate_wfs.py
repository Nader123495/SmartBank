import urllib.request, http.cookiejar, json

N8N_URL = 'http://localhost:5678'
cookie_jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))

# Login
login_data = json.dumps({'emailOrLdapLoginId': 'ngattoussa2002@gmail.com', 'password': 'SmartBank2026!'}).encode()
opener.open(urllib.request.Request(N8N_URL + '/rest/login', data=login_data, headers={'Content-Type': 'application/json'}, method='POST'))
cookies_str = '; '.join([c.name + '=' + c.value for c in cookie_jar])
headers = {'Content-Type': 'application/json', 'Cookie': cookies_str}

# Fetch all workflows
resp = opener.open(urllib.request.Request(N8N_URL + '/rest/workflows', headers=headers))
workflows = json.loads(resp.read().decode()).get('data', [])

# Find Target workflows
target_name = 'SmartBank - Workflow 1: Traitement Nouvelle Réclamation'
wf_list = [w for w in workflows if w.get('name') == target_name]

# Sort by creation date (newest first)
wf_list.sort(key=lambda x: x.get('createdAt', ''), reverse=True)

if not wf_list:
    print('Aucun workflow trouve.')
else:
    newest_wf = wf_list[0]
    print(f'Nouveau workflow identifie : {newest_wf.get("id")}')

    # Deactive and delete all OLD workflows
    for wf in wf_list[1:]:
        wid = wf.get('id')
        print(f'Desactivation et suppression de l ancien workflow {wid}...')
        try:
            # 1. Get current workflow details to preserve other fields
            det_resp = opener.open(urllib.request.Request(f'{N8N_URL}/rest/workflows/{wid}', headers=headers))
            full_wf = json.loads(det_resp.read().decode()).get('data', {})
            full_wf['active'] = False
            # Deactivate
            req_put = urllib.request.Request(f'{N8N_URL}/rest/workflows/{wid}', data=json.dumps(full_wf).encode(), headers=headers, method='PUT')
            opener.open(req_put)
            # Delete
            req_del = urllib.request.Request(f'{N8N_URL}/rest/workflows/{wid}', headers=headers, method='DELETE')
            opener.open(req_del)
            print(f' -> SUPPRIME : {wid}')
        except Exception as e:
            print(f' Error Deleting {wid}: {e}')

    # Activate NEW workflow
    try:
        wid = newest_wf.get('id')
        det_resp = opener.open(urllib.request.Request(f'{N8N_URL}/rest/workflows/{wid}', headers=headers))
        full_wf = json.loads(det_resp.read().decode()).get('data', {})
        full_wf['active'] = True
        req_put = urllib.request.Request(f'{N8N_URL}/rest/workflows/{wid}', data=json.dumps(full_wf).encode(), headers=headers, method='PUT')
        opener.open(req_put)
        print(f'Nouveau workflow ACTIVE avec succes : {wid} !')
    except Exception as e:
        print(f' Error Activating {wid}: {e}')
