import urllib.request, http.cookiejar, json

N8N_URL = 'http://localhost:5678'
NEW_CRED_ID = 'BSklPeVUMqJtRmZX'
NEW_CRED_NAME = 'smartbank-smtp'

cookie_jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))
login_data = json.dumps({'emailOrLdapLoginId': 'ngattoussa2002@gmail.com', 'password': 'SmartBank2026!'}).encode()
opener.open(urllib.request.Request(N8N_URL + '/rest/login', data=login_data, headers={'Content-Type': 'application/json'}, method='POST'))
cookies_str = '; '.join([c.name + '=' + c.value for c in cookie_jar])
headers = {'Content-Type': 'application/json', 'Cookie': cookies_str}

# Get all workflows
wf_list = json.loads(opener.open(urllib.request.Request(N8N_URL + '/rest/workflows', headers=headers)).read().decode()).get('data', [])
print('Workflows:', [w['name'] for w in wf_list])

for wf_meta in wf_list:
    wf_id = wf_meta['id']
    wf = json.loads(opener.open(urllib.request.Request(N8N_URL + '/rest/workflows/' + wf_id, headers=headers)).read().decode()).get('data', {})
    changed = False
    for node in wf['nodes']:
        if node['type'] == 'n8n-nodes-base.emailSend':
            print('Found email node:', node['name'], '| Old creds:', node.get('credentials', {}))
            node['credentials'] = {'smtp': {'id': NEW_CRED_ID, 'name': NEW_CRED_NAME}}
            changed = True
    if changed:
        try:
            put_req = urllib.request.Request(N8N_URL + '/rest/workflows/' + wf_id, data=json.dumps(wf).encode(), headers=headers, method='PUT')
            opener.open(put_req)
            print('[DONE] Workflow "' + wf['name'] + '" updated with correct SMTP credential!')
        except Exception as e:
            print('[ERROR] PUT failed:', e)
            # Try PATCH as alternative
            try:
                patch_data = json.dumps({'nodes': wf['nodes']}).encode()
                patch_req = urllib.request.Request(N8N_URL + '/rest/workflows/' + wf_id, data=patch_data, headers={**headers, 'X-HTTP-Method-Override': 'PATCH'}, method='POST')
                opener.open(patch_req)
                print('[DONE via PATCH] Workflow updated!')
            except Exception as e2:
                print('[ERROR] PATCH also failed:', e2)


print('Finished.')
