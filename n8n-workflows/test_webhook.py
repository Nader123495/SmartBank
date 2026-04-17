import urllib.request
import json

# URL du webhook de TEST n8n
url = "http://localhost:5678/webhook-test/complaint/new"

# Les données de la réclamation
data = {
    "clientId": 12345,
    "typeId": 1,
    "clientName": "NADER GATTOUSSA",
    "clientEmail": "ngattoussa2002@gmail.com",
    "description": "Simulation d'un client via un script Python",
    "canal": "Web",
    "agenceId": 1,
    "priorite": "moyen"
}

# Transformer en JSON
json_data = json.dumps(data).encode('utf-8')

# Créer la requête HTTP POST
req = urllib.request.Request(url, data=json_data, headers={'Content-Type': 'application/json'}, method='POST')

print("🚀 Envoi de la réclamation vers n8n...")

# Envoyer et lire la réponse
try:
    with urllib.request.urlopen(req) as response:
        result = json.loads(response.read().decode('utf-8'))
        print("✅ Succès! n8n a répondu :")
        print(json.dumps(result, indent=2, ensure_ascii=False))
except Exception as e:
    print(f"❌ Erreur: {e}")
    print("Vérifiez que vous avez bien cliqué sur 'Execute Workflow' dans n8n avant de lancer ce script !")
