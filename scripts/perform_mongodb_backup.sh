#!/usr/bin/env bash
# This script automates the process of making a mongodump backup
# on linux from one of three nodes in a 3-node replica set.
# You may copy it to e.g. /usr/local/bin/

NC='\033[0m' # No Color
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
datenowstr=$(date +'%Y-%m-%d_%H-%M-%S')
outpath="/var/backups/mongodb/${datenowstr}/"
logpath="mongodb_backup_${datenowstr}.log"

read -r -e -p "Enter database name to backup: " -i "tpp3" dbname

if sudo systemctl is-active --quiet mongod
then
  echo -e "${CYAN}mongod service is still running! It will ${RED}NOT${CYAN} be automatically started again after the backup.${NC}"
else
  echo "mongod service is not running right now. It will temporarily be started to check some things."
  sudo systemctl start mongod
  sleep 3
fi

myhostaddr="$(hostname -I | tr -d '[:space:]'):27017"
mymemberstatus=$(mongo --quiet --eval 'JSON.stringify(rs.status().members)' | jq -r -c ".[] | select(.self).stateStr")
mynumvotes=$(mongo --quiet --eval 'JSON.stringify(rs.conf().members)' | jq -r -c ".[] | select(.host == \"$myhostaddr\").votes")
mypriority=$(mongo --quiet --eval 'JSON.stringify(rs.conf().members)' | jq -r -c ".[] | select(.host == \"$myhostaddr\").priority")
allvotes=$(mongo --quiet --eval 'JSON.stringify(rs.conf().members)' | jq -r -c "[.[] | .votes] | add")
allprios=$(mongo --quiet --eval 'JSON.stringify(rs.conf().members)' | jq -r -c "[.[] | .priority] | add")
numsecondaries=$(mongo --quiet --eval 'JSON.stringify(rs.status().members)' | jq -r -c "[.[] | select(.stateStr == \"SECONDARY\")] | length")

if [ "$mymemberstatus" != "SECONDARY" ]; then
  if [ "$mymemberstatus" = "PRIMARY" ]; then
    if [[ $numsecondaries -ge 2 ]]; then
      echo "Detected current node as primary and that there are $numsecondaries secondaries that can take over."
      echo "Automatically stepping down current node..."
      mongo --quiet --eval 'rs.stepDown()'
      echo "Waiting 20 seconds for primary election to finish..."
      sleep 20
      mymemberstatus=$(mongo --quiet --eval 'JSON.stringify(rs.status().members)' | jq -r -c ".[] | select(.self).stateStr")
      if [ "$mymemberstatus" != "SECONDARY" ]; then
        echo -e "${RED}Could not verify that the current node $myhostaddr is in SECONDARY state. Detected '$mymemberstatus'${NC}"
        exit 1
      fi
    else
      echo -e "${RED}Detected current node as primary and that there are only $numsecondaries secondaries online.${NC}"
      echo -e "${RED}Cannot automatically step down current node.${NC}"
      echo -e "${RED}Ensure at least 2 other nodes are healthy and could take over, and try again.${NC}"
      exit 1
    fi
  else
    echo -e "${RED}Could not verify that the current node $myhostaddr is in SECONDARY state. Detected '$mymemberstatus'${NC}"
    exit 1
  fi
fi
echo "Successfully verified that the current node $myhostaddr is in SECONDARY state"

voteswithoutme=$((allvotes - mynumvotes))
if [[ $mynumvotes -ge $voteswithoutme ]]; then
  echo -e "${RED}Could not verify that the replica set would have a majority of votes without this node.${NC}"
  echo -e "${RED}Detected this node having '$mynumvotes' votes, and '$allvotes' votes total${NC}"
  exit 1
fi
echo "Successfully verified that the replica set can have a majority of votes without $myhostaddr"

prioritywithoutme=$(echo "$allprios $mypriority" | awk '{print $1-$2}')
# inverted because awk returns booleans (0=false) instead of exit codes
if ! awk "BEGIN{exit ($mypriority >= $prioritywithoutme)}"; then
  echo -e "${RED}Could not verify that the replica set would have a majority of priority without this node.${NC}"
  echo -e "${RED}Detected this node having '$mypriority' priority, and '$allprios' priority total${NC}"
  exit 1
fi
echo "Successfully verified that the replica set can have a majority priority without $myhostaddr"

echo "Shutting down mongod service..."
sudo systemctl stop mongod
sleep 3

echo "Launching mongodb service with custom config for backup..."
sudo touch "$logpath" && sudo chown mongodb:mongodb "$logpath" \
&& sudo -u mongodb mongod --dbpath=/var/lib/mongodb --bind_ip=127.0.0.1 --port=27018 --logappend --logpath="$logpath" &
echo -e "${CYAN}If you cancel this script now, you need to manually stop the mongodb instance running on port 27018 first!${NC}"
echo -e "${CYAN}To do that, run: mongo --port 27018 admin --eval \"db.shutdownServer();\"${NC}"
sleep 10

echo -e "Starting backup of database ${CYAN}$dbname${NC}, target path is $outpath"
sudo mkdir "$outpath" \
&& sudo mongodump --port=27018 --gzip --db="$dbname" --out="$outpath" \
&& echo -e "${GREEN}Backup finished of database '$dbname'! Saved to ${outpath}${NC}"
backup_success=$?

echo "Stopping mongodb server running on port 27018 ..."
mongo --port 27018 admin --eval "db.shutdownServer();" > /dev/null
sleep 3

if [ "$backup_success" != "0" ]; then
  echo -e "${RED}Backup failed! Check the above output for details! regular mongod service was not started again.${NC}"
  exit 1
fi

echo -e "${CYAN}Please assess that the backup looks successful and complete.${NC}"
echo -e "${CYAN}If you are certain, you may start the regular database service again.${NC}"
echo -e "${CYAN}To do that, run: sudo systemctl start mongod${NC}"
