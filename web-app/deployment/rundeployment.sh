#!/usr/bin/env bash

# Creates the resource group that will contain all of our application resources
az group create -n $RGNAME_WA -l $RGLOCATION_WA

# Creates the SQL Database Server and Database with the provided admin details
# This is not created in the ARM template below because it's assumed that the lifecycle
# of this SQL Database is longer than that of this web application
az sql server create -l $RGLOCATION_WA -g $RGNAME_WA -n $SQLSERVERNAME_WA -u $SQLADMINUSER_WA -p $SQLADMINPASSWORD_WA -e true --minimal-tls-version 1.2
az sql db create -s $SQLSERVERNAME_WA -n $SQLSERVERDB_WA -g $RGNAME_WA

# Set the firewall to allow ALL connections (This would actually be set to just your web app, or better yet, the SQL Database is exposed only via Private Endpoints.)
az sql server firewall-rule create -g $RGNAME_WA -s $SQLSERVERNAME_WA -n azureservices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# Populate the schema in the database (This would normally be handled via your DB Schema management solution)

# If you are running from a location other than Azure Cloud Shell, you may need to temporarily add your IP address
# to the sql firewall (see the commented out lines below)
#shell_ip=$(curl ifconfig.io)
#az sql server firewall-rule create -g $RGNAME_WA -s $SQLSERVERNAME_WA -n shell --start-ip-address $shell_ip --end-ip-address $shell_ip
sleep 20s
sqlcmd -S tcp:${SQLSERVERNAME_WA}.database.windows.net,1433 -d $SQLSERVERDB_WA -U $SQLADMINUSER_WA -P $SQLADMINPASSWORD_WA -N -l 30 -Q "CREATE TABLE Counts(ID INT NOT NULL IDENTITY PRIMARY KEY, Candidate VARCHAR(32) NOT NULL, Count INT)"
#az sql server firewall-rule delete -g $RGNAME_WA -s $SQLSERVERNAME_WA -n shell

# Deploy those resources found in the ARM template - This deploys the bulk of the resources.
connstring=$(az sql db show-connection-string -s $SQLSERVERNAME_WA -n $SQLSERVERDB_WA -c ado.net | sed "s/<username>/${SQLADMINUSER_WA}/;s/<password>/${SQLADMINPASSWORD_WA}/")
sqlcon="${connstring%\"}"
sqlcon="${sqlcon#\"}"
az deployment group create -g $RGNAME_WA -u ${DEPLOYMENT_WA}webappdeploy.json -p VotingWeb_name=${DNSNAME_WA} SqlConnectionString="${sqlcon}"