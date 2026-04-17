import requests
import json

url = "http://localhost:5678/webhook-test/complaint/new"
payload = {
    "clientId": 123,
    "typeId": 1,
    "clientName": "John Doe",
    "clientEmail": "john@example.com",
    "description": "Ma carte est bloquée",
    "canal": "e-banking",
    "agenceId": 1,
    "priorite": "Haute"
}

headers = {
    "Content-Type": "application/json"
}

try:
    response = requests.post(url, data=json.dumps(payload), headers=headers)
    print(f"Status Code: {response.status_code}")
    print(f"Response Body: {response.text}")
except Exception as e:
    print(f"Error: {e}")
