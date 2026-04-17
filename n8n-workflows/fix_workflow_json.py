import json

with open('workflow1_nouvelle_reclamation.json', 'r', encoding='utf-8') as f:
    wf = json.load(f)

for node in wf.get('nodes', []):
    
    # 1. Update SQL "Récupérer Charge des Agents"
    if node['name'].startswith('R\u00e9cup\u00e9rer Charge des Agents'):
        node['parameters']['query'] = """SELECT 
  u.Id AS agentId,
  u.FirstName AS Nom,
  u.LastName AS Prenom,
  u.Email AS agentEmail,
  COUNT(c.Id) AS chargeActuelle
FROM Users u
LEFT JOIN Complaints c ON c.AssignedToUserId = u.Id AND c.Status != 'Closed'
WHERE u.AgencyId = 1
AND u.IsActive = 1
GROUP BY u.Id, u.FirstName, u.LastName, u.Email
ORDER BY chargeActuelle ASC"""

    # 2. Update SQL "Mettre à Jour BDD + SLA"
    if node['name'].startswith('Mettre \u00e0 Jour BDD'):
        node['parameters']['query'] = """UPDATE Complaints
SET AssignedToUserId = {{ $json.agentId }},
    Status = 'Assigned',
    UpdatedAt = GETUTCDATE()
WHERE Id = {{ $('Ins\u00e9rer R\u00e9clamation SQL').item.json.Id }};

INSERT INTO SLA (ComplaintId, SlaHeures, DateDebut, DateLimite, Statut)
VALUES (
    {{ $('Ins\u00e9rer R\u00e9clamation SQL').item.json.Id }}, 
    {{ $('Valider & Enrichir Donn\u00e9es').item.json.slaHeures }}, 
    GETUTCDATE(), 
    '{{ $('Valider & Enrichir Donn\u00e9es').item.json.slaDueDate }}', 
    'EnCours'
);"""

    # 3. Update Email node recipient
    if node['type'] == 'n8n-nodes-base.emailSend':
        node['parameters']['toEmail'] = "={{ $('S\u00e9lectionner Meilleur Agent').item.json.agentEmail }}"


with open('workflow1_nouvelle_reclamation.json', 'w', encoding='utf-8') as f:
    json.dump(wf, f, indent=2, ensure_ascii=False)

print("FILE UPDATED SUCCESSFULLY!")
