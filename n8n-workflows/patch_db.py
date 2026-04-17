import sqlite3, json, os

DB_PATH = r'C:\Users\msi\.n8n\database.sqlite'
WF_FILE = r'c:\Users\msi\Downloads\SmartBank\n8n-workflows\workflow2_sla_escalade.json'

def patch_db():
    try:
        with open(WF_FILE, 'r', encoding='utf-8') as f:
            local_wf = json.load(f)
        
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # On cherche par nom car l'ID peut varier
        nodes_json = json.dumps(local_wf['nodes'])
        connections_json = json.dumps(local_wf['connections'])
        name = local_wf['name']
        
        # Test de l'ID spécifique vu dans le browser
        target_id = 'UvPqgIL1FXTILX3h'
        
        # On essaie d'abord par l'ID
        cursor.execute("SELECT id FROM workflow_entity WHERE id = ?", (target_id,))
        row = cursor.fetchone()
        
        if row:
            cursor.execute("""
                UPDATE workflow_entity 
                SET nodes = ?, connections = ?, name = ?, active = 1, updatedAt = datetime('now')
                WHERE id = ?
            """, (nodes_json, connections_json, name, target_id))
            print(f"Workflow ID {target_id} mis à jour dans SQLite !")
        else:
            # Sinon par nom
            cursor.execute("""
                UPDATE workflow_entity 
                SET nodes = ?, connections = ?, active = 1, updatedAt = datetime('now')
                WHERE name LIKE '%Workflow 2%'
            """, (nodes_json, connections_json))
            print("Workflow mis à jour par nom dans SQLite !")
            
        conn.commit()
        conn.close()
        print("Injection terminée avec succès !")
        
    except Exception as e:
        print(f"Erreur lors de l'injection : {e}")

if __name__ == "__main__":
    patch_db()
