import os

base_dir = r"C:\Users\msi\Downloads\SmartBank\n8n-workflows"
files = ["workflow1_nouvelle_reclamation.json", "workflow2_sla_escalade.json", "workflow3_statut_notifications.json"]

for file in files:
    path = os.path.join(base_dir, file)
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8") as f:
            content = f.read()
        
        content = content.replace("n8n-nodes-base.mssql", "n8n-nodes-base.microsoftSql")
        content = content.replace('"mssql": {', '"microsoftSql": {')
        
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"Correction appliquee sur: {file}")
