ce projet permet de pouvoir transformer completer de la documentation technique et faire des recherche via une AI sur celle ci en toute confidentialité car tout est en local est sécurisé.
Pour changer le modèle il suffit de changer le nom du modèle dans le fichier appsettings.json voici un panorama des modèles disponibles : https://ollama.com/models
# 🧠 Panorama des modèles disponibles dans Ollama

## 📋 Vue d’ensemble

| Modèle                  | Multimodal | Taille approx. | RAM/VRAM min.         | Utilisation principale                              |
|-------------------------|------------|----------------|------------------------|-----------------------------------------------------|
| **LLaVA 7B**            | ✅ Oui     | ~4.5 Go        | ~8 Go                  | Texte + image léger, très rapide                   |
| **Gemma 3 1B / 4B / 12B / 27B** | ✅ Oui | 0.8–17 Go     | 4B : ~3.3 Go<br>12B : ~8 Go<br>27B : ~17 Go | Excellente compréhension, long contexte (128K)     |
| **Mistral 7B**          | ❌ Non     | ~4.1 Go        | ~8 Go                  | Génération de texte rapide et fiable               |
| **LLaMA 2 7B**          | ❌ Non     | ~3.8 Go        | ~8 Go                  | Bon généraliste en génération de texte             |
| **Phi-3 Mini (3.8B)**   | ❌ Non     | ~2.3 Go        | ~4–6 Go                | Ultra léger, rapide sur machines peu puissantes    |
| **CodeLLaMA 7B**        | ❌ Non     | ~3.8 Go        | ~8 Go                  | Génération de code (C, Python, etc.)               |

---

## 🚀 Performances observées

### ✅ Exemples sur Mac M4 Max
| Modèle (Quantisation Q4) | Vitesse approx. |
|--------------------------|-----------------|
| **Gemma 3 4B**           | ~98 tok/s       |
| **Gemma 3 12B**          | ~44 tok/s       |
| **Gemma 3 27B**          | ~22 tok/s       |

---

### ⚠️ Consommation mémoire (RAM + VRAM)

- **Gemma 3 27B** :  
  - GPU VRAM ~21 Go  
  - RAM ~18 Go  
  ([Source](https://github.com/ollama/ollama/issues/9701))

- **Gemma 3 12B** :  
  - GPU VRAM ~8 Go  
  - RAM ~16 Go  
  ([Source](https://www.reddit.com/r/ollama/comments/1jaydvn))

- **Mistral 7B sur CPU sans GPU** :  
  - Lent (~12 s par prompt sur iGPU Vega 8)  
  ([Source](https://www.reddit.com/r/ollama/comments/1jhmldw))

---

## 🧠 Recommandations

| Besoin                                | Modèle recommandé       |
|---------------------------------------|--------------------------|
| Texte + image, faible RAM             | **LLaVA 7B**             |
| Texte seul, ultra léger               | **Phi‑3 Mini**           |
| Texte + image + long contexte (128K) | **Gemma 3 4B ou 12B**    |
| Génération de code                    | **CodeLLaMA 7B**         |
| Meilleur rapport perf/RAM             | **Mistral 7B Q4_K_M**    |

---

## 💬 Retours utiles

> "Gemma 3 4B has good vision capabilities as well as decent wordsmithing."  
> — [Reddit](https://www.reddit.com/r/LocalLLaMA/comments/1jf7tng)

> "Gemma 3 27B with Q4_K_M quantization fits under 32GB."  
> — [Reddit](https://www.reddit.com/r/ollama/comments/1jlqee3)

---

## 📚 Liens utiles

- [📘 Liste officielle des modèles supportés – Ollama Operator](https://ollama-operator.ayaka.io/pages/en/guide/supported-models)
- [📚 Gemma 3 benchmarks – ZazenCodes](https://zazencodes.com/blog/ultimate-gemma3-ollama-guide-testing-1b-4b-12b-27b/)
- [📄 Gemma 3 paper – arXiv](https://arxiv.org/abs/2503.19786)

---
### Attention au modele choisi cette API a besoin d'une API multimodale analyse d'image et de texte pour fonctionner car elle a besoin de faire une description des images.

###Installation de Ollama
Lancement du docker compose pour Ollama  :

les AI qui consomment peux n'ont pas besoin de cartes graphiques par exemple pour un mistral 7b il est préférable d'avoir 2-4cpu et 8go de ram.
cependant si vous avez une carte graphique il serait mieux d'installer le driver permettant de l'utiliser.
Une 3070 serait parfaite pour faire tourner les modèles de 7b et 13b.
for windows download cuda driver for use GPU :  https://developer.nvidia.com/cuda-downloads

### Changement du modèle
pour tout changement du modèle il suffit de changer le nom du modèle dans le fichier /script/entrypoint.sh
et aussi de changer les ressources cpu et ram dans le fichier appsettings.json (il faut aller voir combien de ressource votre modèle consomme sur le site d'ollama)
on peut aussi avoir plusieurs AI
### Prérequis
docker compose up
le modele peut mettre assez longtemps à charger 5-10 mn

### Lancement du serveur
 dotnet run --launch-profile https

