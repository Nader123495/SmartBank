# Documentation UML complète pour le projet SmartBank

Cette documentation contient tous les diagrammes de classes, les diagrammes de séquence, les cas d'utilisation et des explications détaillées.

## Diagrammes de classes

### Classes principales
1. **Classe Client** :
   - Attributs : `idClient`, `nom`, `prenom`, `email`, `telephone`.
   - Méthodes : `creerCompte()`, `modifierDetails()`, `supprimerCompte()`.

2. **Classe Compte** :
   - Attributs : `idCompte`, `solde`, `typeCompte`, `dateOuverture`.
   - Méthodes : `deposerArgent()`, `retirerArgent()`, `afficherSolde()`.

### Diagramme de classes
![Diagramme de classes](link_vers_diagramme_de_classes)

## Diagrammes de séquence

### Séquence d'ouverture d'un compte
1. Le client remplit le formulaire d'ouverture de compte.
2. La demande est envoyée au système.
3. Le système crée un nouveau compte et confirme l'opération au client.

![Diagramme de séquence](link_vers_diagramme_de_sequence)

## Cas d'utilisation

### Cas d'utilisation : Ouvrir un compte
- **Acteur** : Client
- **Précondition** : Le client doit avoir fourni les informations nécessaires.
- **Postcondition** : Un nouveau compte est créé.

### Description
Le client remplit un formulaire de demande. Après validation des informations, le compte est créé dans le système.

## Explications détaillées

### Classes expliquées
- **Client** : Représente les clients de SmartBank et gère leurs opérations.
- **Compte** : Gère les opérations financières des clients, telles que les dépôts et les retraits.

### Recommandations
- Suivre les bonnes pratiques de modélisation UML.
- Mettre à jour les diagrammes au fur et à mesure des modifications apportées au projet.