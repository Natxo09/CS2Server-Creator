@echo off
setlocal

:: Directorio donde se encuentra el repositorio git y tu trabajo principal
set REPO_DIR=D:\CODE\CS2ServerCreatorGIT

cd D:

cd "%REPO_DIR%"

:: Inicializa el repositorio si aún no se ha hecho
if not exist .git (
    git init
    git remote add origin https://github.com/Natxo09/CS2Server-Creator.git
)

:: Añade todos los cambios al área de preparación
git add .

:: Realiza un commit con los cambios
git commit -m "Automatic Update"

:: Fuerza el push a la rama principal (main) del repositorio remoto
git push -f origin main

echo Proceso finalizado.
pause

endlocal
