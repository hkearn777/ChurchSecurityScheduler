@echo off
echo Deploy th Church Security Scheduler to Google
pause

echo Working...

cd /d "C:\Users\Howard\Documents\VSProjects\ChurchSecurityScheduler"
call gcloud run deploy church-security-scheduler --source . --region us-central1 --allow-unauthenticated --platform managed --set-secrets=GOOGLE_CREDENTIALS_JSON=sheets-credentials:latest

echo Deploy complete, look for any errors
pause