import json
import os
import urllib.request
import http.cookiejar

base_dir = r'C:\Users\msi\Downloads\SmartBank\n8n-workflows'
p1 = os.path.join(base_dir, 'workflow1_nouvelle_reclamation.json')

with open(p1, 'r', encoding='utf-8') as f:
    wf1 = json.load(f)

for node in wf1['nodes']:
    if node['name'] == 'Insérer Réclamation SQL':
        node['parameters']['query'] = """INSERT INTO Complaints (ClientName, ClientEmail, ComplaintTypeId, Title, Description, Channel, AgencyId, Priority, Status, SLADeadline, CreatedAt, Reference, IsEscalated)
OUTPUT INSERTED.Id
VALUES (
  '{{ $json.clientName }}',
  '{{ $json.clientEmail }}',
  {{ $json.typeId }},
  'Nouvelle Reclamation n8n',
  '{{ $json.description }}',
  '{{ $json.canal }}',
  {{ $json.agenceId }},
  '{{ $json.priorite }}',
  'Pending',
  '{{ $json.slaDueDate }}',
  '{{ $json.dateCreation }}',
  '{{ $json.refReclamation }}',
  0
)"""

with open(p1, 'w', encoding='utf-8') as f:
    json.dump(wf1, f, indent=2, ensure_ascii=False)

N8N_URL = 'http://localhost:5678'
cookie_jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))
login_data = json.dumps({'emailOrLdapLoginId': 'ngattoussa2002@gmail.com', 'password': 'SmartBank2026!'}).encode()
opener.open(urllib.request.Request(f'{N8N_URL}/rest/login', data=login_data, headers={'Content-Type': 'application/json'}, method='POST'))
cookies_str = '; '.join([f'{c.name}={c.value}' for c in cookie_jar])
headers = {'Content-Type': 'application/json', 'Cookie': cookies_str}

wf_list = json.loads(opener.open(urllib.request.Request(f'{N8N_URL}/rest/workflows', headers=headers)).read().decode()).get('data', [])
wf = next((w for w in wf_list if 'Workflow 1' in w['name']), None)
wf1['active'] = True
opener.open(urllib.request.Request(f'{N8N_URL}/rest/workflows/' + str(wf['id']), data=json.dumps(wf1).encode(), headers=headers, method='PUT'))
print('SQL Insert fixed and Workflow pushed!')
