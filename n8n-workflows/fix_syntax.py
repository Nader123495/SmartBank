import json

with open('workflow1_nouvelle_reclamation.json', 'r', encoding='utf-8') as f:
    wf = json.load(f)

for node in wf.get('nodes', []):
    if 'Notifier' in node['name']:
        node['parameters']['url'] = 'http://localhost:5000/api/integrations/n8n/complaints/{{$json.complaintId}}/notify'
        
        for b in node['parameters'].get('bodyParameters', {}).get('parameters', []):
            if b['name'] == 'message':
                b['value'] = "={{ 'Réclamation ' + $json.refReclamation + ' vous a été assignée. Priorité: ' + $json.priorite }}"

with open('workflow1_nouvelle_reclamation.json', 'w', encoding='utf-8') as f:
    json.dump(wf, f, indent=2, ensure_ascii=False)

print('FIXED')
