#!/bin/sh
ollama serve &
sleep 5
ollama run llava:7b || true
wait