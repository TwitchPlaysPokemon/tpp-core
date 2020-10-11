#!/usr/bin/env bash
# This script automates the process of making a mongodump backup
# on linux from one of two nodes in a 2-node replica set.
# You may copy it to e.g. /usr/local/bin/

NC='\033[0m' # No Color
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
datenowstr=$(date +'%Y-%m-%d_%H-%M-%S')
outpath="/var/backups/mongodb/${datenowstr}/"
logpath="mongodb_backup_${datenowstr}.log"

if sudo systemctl is-active --quiet mongod
then
  echo -e "${CYAN}mongod service is still running! It will ${RED}NOT${CYAN} be automatically started again after the backup.${NC}"
else
  echo "mongod service is not running right now. It will temporarily be started to check some things."
  sudo systemctl start mongod
  sleep 3
fi

myhostaddr="$(hostname -I | tr -d '[:space:]'):27017"
mymemberstatus=$(mongo --quiet --eval 'JSON.stringify(rs.status().members)' \
| jq -r -c ".[] | select(.name == \"$myhostaddr\").stateStr")
mynumvotes=$(mongo --quiet --eval 'JSON.stringify(rs.conf().members)' \
| jq -r -c ".[] | select(.host == \"$myhostaddr\").votes")
mypriority=$(mongo --quiet --eval 'JSON.stringify(rs.conf().members)' \
| jq -r -c ".[] | select(.host == \"$myhostaddr\").priority")

if [ "$mymemberstatus" != "SECONDARY" ]; then
  echo -e "${RED}Could not verify that the current node $myhostaddr is in SECONDARY state. Detected '$mymemberstatus'${NC}"
  exit 1
fi
echo "Successfully verified that the current node $myhostaddr is in SECONDARY state"

if [ "$mynumvotes" != "0" ]; then
  echo -e "${RED}Could not verify that the current node $myhostaddr has 0 votes. Detected '$mynumvotes'${NC}"
  exit 1
fi
echo "Successfully verified that the current node $myhostaddr has 0 votes"

if [ "$mypriority" != "0" ]; then
  echo -e "${RED}Could not verify that the current node $myhostaddr has 0 votes. Detected '$mypriority'${NC}"
  exit 1
fi
echo "Successfully verified that the current node $myhostaddr has 0 priority"

echo "Shutting down mongod service..."
sudo systemctl stop mongod
sleep 3

echo "Launching mongodb service with custom config for backup..."
sudo touch "$logpath" && sudo chown mongodb:mongodb "$logpath" \
&& sudo -u mongodb mongod --dbpath=/var/lib/mongodb --bind_ip=127.0.0.1 --port=27018 --logappend --logpath="$logpath" &
echo -e "${CYAN}If you cancel this script now, you need to manually stop the mongodb instance running on port 27018 first!${NC}"
echo -e "${CYAN}To do that, run: mongo --port 27018 admin --eval \"db.shutdownServer();\"${NC}"
sleep 10

echo "Starting backup, target path is $outpath"
sudo mkdir "$outpath" \
&& sudo mongodump --port=27018 --gzip --out="$outpath" \
&& echo -e "${GREEN}Backup finished! Saved to ${outpath}${NC}"
backup_success=$?

echo "Stopping mongodb server running on port 27018 ..."
mongo --port 27018 admin --eval "db.shutdownServer();" > /dev/null
sleep 3

if [ "$backup_success" != "0" ]; then
  echo -e "${RED}Backup failed! Check the above output for details! regular mongod service was not started again.${NC}"
  exit 1
fi

echo "${CYAN}Please assess that the backup looks successful and complete.${NC}"
echo "${CYAN}If you are certain, you may start the regular database service again.${NC}"
echo "${CYAN}To do that, run: sudo systemctl start mongod${NC}"
