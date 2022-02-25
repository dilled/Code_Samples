'use strict';
var serverVersion; // for preventing old game builds to connect

require('dotenv').config()

const express = require('express');
const socketIO = require('socket.io');
const PORT = process.env.PORT || 8123;

const server = express()
  .use((req, res) => res.sendFile(INDEX, { root: __dirname }))
  .listen(PORT, () => console.log(`Listening on ${PORT}`));

console.log('Server started');

var defaultCity = require('./db/test3.json'); // just to get something to the Postgresql if DB is empty

const io = socketIO(server, { //8124 is the local port we are binding the pingpong server to
  pingInterval: 30005,		//An interval how often a ping is sent
  pingTimeout: 5000,		//The time a client has to respont to a ping before it is desired dead
  upgradeTimeout: 3000,		//The time a client has to fullfill the upgrade
  allowUpgrades: true,		//Allows upgrading Long-Polling to websockets. This is strongly recommended for connecting for WebGL builds or other browserbased stuff and true is the default.
  cookie: false,			//We do not need a persistence cookie for the demo - If you are using a load balöance, you might need it.
  serveClient: true,		//This is not required for communication with our asset but we enable it for a web based testing tool. You can leave it enabled for example to connect your webbased service to the same server (this hosts a js file).
  allowEIO3: false,			//This is only for testing purpose. We do make sure, that we do not accidentially work with compat mode.
  cors: {
    origin: "*"				//Allow connection from any referrer (most likely this is what you will want for game clients - for WebGL the domain of your sebsite MIGHT also work)
  }});
var serverReady = false; 
var amountOfRandomCities = 10; // Amount of cities randomized if db is empty

var shortid = require('shortid');
var players = [];
var worldCities = [];
var npcPlayers = [];

var cityLocations = [];
var freeCityLocations = [];
var cityLocStep = 200;
var closestDist = 150;

async function randomCitiesToDB(amount, cityName){
  if(freeCityLocations.length<amount){
    randomCityLocation(amount, true);
  }
  for(var i=0;i<amount;i++){
    var npcPayloadData = await getNPCPayloadData(defaultCity.farmers, defaultCity.army, defaultCity.slaves);
    var random = Math.floor(Math.random() * freeCityLocations.length);
    var randomCityId = shortid.generate();
    var randomCity = {
      ownerID: randomCityId,
      population: defaultCity.population,
      army: defaultCity.army,
      farmers: defaultCity.farmers,
      slaves: defaultCity.slaves,
      resource: defaultCity.resource,
      buildingPayloadData: defaultCity.buildingPayloadData,
      cityName: cityName+i,
      worldLocation: freeCityLocations[random],
      npcPayloadData: npcPayloadData,
      updated_timestamp: serverStartTime,
    };

    updateDB([true, Buffer.from(JSON.stringify(freeCityLocations[random]))]);
    freeCityLocations.splice(random, 1);
    worldCities[randomCityId] = randomCity; 
    if (psqlBackup){
      let vals = [randomCity.ownerID, randomCity.population, randomCity.army, randomCity.farmers, randomCity.resource, randomCity.buildingPayloadData, randomCity.cityName, randomCity.worldLocation, randomCity.npcPayloadData, randomCity.slaves, randomCity.updated_timestamp];
      addCityToDB(vals);
    }}
  //  console.log('Free City locations: ', freeCityLocations.length);  
}
// Just quick function to get some cities randomized and start trying to use Postgressql on Heroku
function randomCityLocation(amountOfCities, saveToDB) {
  let endInfiniteLoop =0;
  let i = 0;
  var axisX =true;
  
  while(i < amountOfCities){

    let min = -minWorld*worldSizeMultiplier;
    let max = minWorld*worldSizeMultiplier;

    var worldLocation = [Math.floor(Math.random() * (max - min + 1)) + min, 0.01, Math.floor(Math.random() * (max - min + 1)) + min];
    
    var distEnough = true;
    
    for(var loc in freeCityLocations){
    //  console.log(checkCityDistance(freeCityLocations[loc], worldLocation), ' ', i, ' ', endInfiniteLoop)
      if(checkCityDistance(freeCityLocations[loc], worldLocation) > cityLocStep){
        continue;
      }else{
        worldLocation[0] *=-1;
        if (axisX){
        worldLocation[2] =0;
        }
        break;
      }
    }
    for(var loc in freeCityLocations){
    //  console.log(checkCityDistance(freeCityLocations[loc], worldLocation), ' ', i, ' ', endInfiniteLoop)
      if(checkCityDistance(freeCityLocations[loc], worldLocation) > cityLocStep){
        continue;
      }else{
        if(axisX){
          worldLocation[0] =0;
        }
        worldLocation[2] *=-1;
        break;
      }
    }
    axisX = false
    for(var loc in freeCityLocations){
    //  console.log(checkCityDistance(freeCityLocations[loc], worldLocation), ' ', i, ' ', endInfiniteLoop)
      if(checkCityDistance(freeCityLocations[loc], worldLocation) > cityLocStep){
        continue;
      }else{
        worldLocation[0] =0;
        worldLocation[2] *=-1;
        break;
      }
    }
    for(var loc in freeCityLocations){
    //  console.log(checkCityDistance(freeCityLocations[loc], worldLocation), ' ', i, ' ', endInfiniteLoop)
      if(checkCityDistance(freeCityLocations[loc], worldLocation) > cityLocStep){
        continue;
      }else{
        worldLocation[0] *=-1;
        worldLocation[2] =0;
        
        break;
      }
    }
    for(var loc in freeCityLocations){
    //  console.log(checkCityDistance(freeCityLocations[loc], worldLocation), ' ', i, ' ', endInfiniteLoop)
      if(checkCityDistance(freeCityLocations[loc], worldLocation) > cityLocStep){
        continue;
      }else{
        distEnough = false;
        axisX = true;
        break;
      }
    }
        
    if(distEnough){
      freeCityLocations.push(worldLocation);
      if(saveToDB){
        var values = [worldLocation, false];
        freeCityLocationsToDB(values);
      }
      i++;
    }else{
      endInfiniteLoop+=1
    }
    if(endInfiniteLoop > 1000){
      worldSizeMultiplier +=.1;
      origonSize *=worldSizeMultiplier
      endInfiniteLoop=0
    }
  }
  return worldLocation;
}

function checkCityDistance(pl1, pl2){
  var deltaX = parseFloat(pl1[0]) - parseFloat(pl2[0]);
  var deltaY = parseFloat(pl1[1]) - parseFloat(pl2[1]);
  var deltaZ = parseFloat(pl1[2]) - parseFloat(pl2[2]);
  var distance = Math.sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
  //console.log(distance);
  if(distance<closestDist){
    closestDist = distance;
  }
  return distance;
}

function AddNPCPosition(position) { 
      var thisNPCId = shortid.generate();
      var npc = {
          id: thisNPCId,
          location: position,
          target: [],
        }
          npcPlayers[thisNPCId] = npc;    
}

async function getNPCPayloadData(farmers, armys, slaves) {
  var currentPayloadData = [];
  for (var i = 0; i<farmers; i++){
    var thisNPCId = shortid.generate();
    var npc = {
        id: thisNPCId,
        name: "Farmer",
        level: 1,
        health: 100.0,
        speed: 2,
        strength: 10,
        location: [0,0,0],
        rotation: [0,0,0,0],
        target: [0,0,0],
        npcPrefab: 0,
    }
    currentPayloadData.push(npc);
    npcPlayers[thisNPCId] = npc;
  }
  for (var i = 0; i<armys; i++){
    var thisNPCId = shortid.generate();
    var npc = {
        id: thisNPCId,
        name: "Soldier",
        level: 1,
        health: 100.0,
        speed: 3,
        strength: 10,
        location: [0,0,0],
        rotation: [0,0,0,0],
        target: [0,0,0],
        npcPrefab: 1,
    }
    currentPayloadData.push(npc);
    npcPlayers[thisNPCId] = npc;
  }
  for (var i = 0; i<slaves; i++){
    var thisNPCId = shortid.generate();
    var npc = {
        id: thisNPCId,
        name: "Slave",
        level: 1,
        health: 100.0,
        speed: 3,
        strength: 10,
        location: [0,0,0],
        rotation: [0,0,0,0],
        target: [0,0,0],
        npcPrefab: 2,
    }
    currentPayloadData.push(npc);
    npcPlayers[thisNPCId] = npc;
  }
  //console.log(currentPayloadData);
  return currentPayloadData;
}
async function getCityNPCs(npcPayloadData){

  for(var npc in npcPayloadData){
  
    npcPlayers[npcPayloadData[npc].id] = npcPayloadData[npc];
    //console.log('NPC ', npcPayloadData[npc])
  }
  //console.log('npcPayloadData ', npcPayloadData)
}

var currentClients = 0;
var maxClients =0;


const moveRoom = (socket, from, to) => {
  socket.leave(from);
  //to = 'lobby';
  socket.join(to);
  console.log(from, ' from to new room ', to);
  console.log(io.sockets.adapter.rooms);
}
function sleep(ms) {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}  
function calculateResources(timePassedMins, farmers, slaves, resource){
  var newResourceAmount = slaves*timePassedMins+resource; 
  newResourceAmount = Math.round(newResourceAmount);
  return newResourceAmount;
}

function timeDifference(date1,date2) {
  var difference = date1.getTime() - date2.getTime();

  var daysDifference = Math.floor(difference/1000/60/60/24);
  difference -= daysDifference*1000*60*60*24

  var hoursDifference = Math.floor(difference/1000/60/60);
  difference -= hoursDifference*1000*60*60

  var minutesDifference = Math.floor(difference/1000/60);
  difference -= minutesDifference*1000*60

  var secondsDifference = Math.floor(difference/1000);

  console.log('difference = ' + 
    daysDifference + ' day/s ' + 
    hoursDifference + ' hour/s ' + 
    minutesDifference + ' minute/s ' + 
    secondsDifference + ' second/s ');
}

io.on('connection', (socket) => {
  var serverTimeNow = new Date().toUTCString();
  var thisPlayerId = shortid.generate();
  var playerCityId ='';
  /*var playerCity = {
    ownerID: playerCityId,
    population: defaultCity.population,
    army: defaultCity.army,
    farmers: defaultCity.farmers,
    slaves: defaultCity.slaves,
    resource: defaultCity.resource,
    buildingPayloadData: [],
    cityName: '',
    worldLocation: [], //randomCityLocation(),//defaultCity.worldLocation,
    npcPayloadData: [],
    updated_timestamp: serverTimeNow,
  };*/
  //console.log('default city', defaultCity);
  console.log('Client connected', thisPlayerId);
  //socket.emit('getType');
  //moveRoom(socket, socket.id, room);
//  socket.join(room);
  socket.on('join', function(data) {
      console.log(data);
      socket.join(data.room);
      console.log('rooms: ', io.sockets.adapter.rooms);
  });
  currentClients = io.engine.clientsCount;
  totalClients ++;
  if (maxClients < currentClients){
      maxClients = currentClients;
  }
  console.log('Free City locations: ', freeCityLocations.length);
  console.log('Cities in world: ', worldCities.length);
  console.log('cityLocations in world: ', cityLocations.length);
  ///if(freeCityLocations.length < 25){
  //  randomCityLocation(25);
 // }
  //socket.on('disconnect', () => console.log('Client disconnected'));
  
  //socket.emit("BuildScene", playerScene);
  
  /*var player = {
      sid: socket.id,
      id: thisPlayerId,
      cityId: playerCityId,
      location: [],
      rotation: 0.0,
      speed: 0.0,
      verticalVelocity: 0.0,
      inputMove: [],
  }
  players[thisPlayerId] = player;*/
  socket.on('IsServerReady', async (data) => {
    console.log('[' + (new Date()).toUTCString() + '] ' + socket.id + ' IsServerReady... Answering "Who\'s there?"...' + data);      
    if (data == serverVersion){
      socket.emit('ServerMessage', 'Server is still building the world!! Wait..');
      console.log(data)

      while(!serverReady){
        await sleep(1 * 2000);
        console.log('Server is building the world!!..', serverReady, ' ', cityLocations.length);
        if(serverReady){
          break
        }
      }
      socket.emit('ServerMessage', 'Server is ready!');
      socket.emit('ServerIsReady', 'Server is ready!.....');
      console.log('Server is ready!!  worldcities: ', worldCities.length, ' citylocations: ', cityLocations.lenth);
    }else{
      socket.emit('ServerMessage', 'Wrong build version! connection refused!!');
      socket.emit('disconnectPlayer');
    }
    console.log(data, ' ', serverVersion)
  });

  socket.on('KnockKnock', async (data) => {
      console.log('KnockKnock[' + (new Date()).toUTCString() + '] ' + socket.id + ' game knocking... Answering "Who\'s there?"...' + data);      
      
      if(data.ownerID ===""){
        //var defaultCity = getCity('defaultCity');
        //for(var a in freeCityLocations){
          
        var npcPayloadData = await getNPCPayloadData(defaultCity.farmers, defaultCity.army, defaultCity.slaves);
        var random = Math.floor(Math.random() * freeCityLocations.length);
        playerCityId = shortid.generate();
        var playerCity = {
          ownerID: playerCityId,
          population: defaultCity.population,
          army: defaultCity.army,
          farmers: defaultCity.farmers,
          slaves: defaultCity.slaves,
          resource: defaultCity.resource,
          buildingPayloadData: defaultCity.buildingPayloadData,
          cityName: data.cityName,
          worldLocation: freeCityLocations[random], //randomCityLocation(),//defaultCity.worldLocation,
          npcPayloadData: npcPayloadData,
          updated_timestamp: serverTimeNow,
        };
        updateDB([true, Buffer.from(JSON.stringify(freeCityLocations[random]))]);
        freeCityLocations.splice(random, 1);

        worldCities[playerCityId] = playerCity;
        
        if (psqlBackup){
          let vals = [playerCity.ownerID, playerCity.population, playerCity.army, playerCity.farmers, playerCity.resource, playerCity.buildingPayloadData, playerCity.cityName, playerCity.worldLocation, playerCity.npcPayloadData, playerCity.slaves, playerCity.updated_timestamp];
          addCityToDB(vals);

        }
        var newCityData  = {"cities": [worldCities[playerCityId]]};
        io.emit('WorldData', newCityData);
        //}
      }else{
        playerCityId = data.ownerID;
        var playerCity = worldCities[playerCityId];
        if (playerCity){
          console.log('City found on server!')
          
        }else{
          playerCity = await getCity([data.ownerID]);
          if (playerCity){
            console.log('City found on Database!')
            
          }else{
            console.log('City NOT found on Server or Database!')
            socket.emit('ServerMessage', 'City NOT found on Server or Database!');
            return
          }
        }
        var date1 = new Date(serverTimeNow);
        var date2 = new Date(playerCity.updated_timestamp);
        console.log(playerCity)
        console.log(date1, ' ', date2)
        var timePassed = (date1 - date2)/(1000*60);
        playerCity.updated_timestamp = serverTimeNow;
        console.log(timePassed);
        playerCity.resource = calculateResources(timePassed, playerCity.farmers, playerCity.slaves, playerCity.resource);
        timeDifference(date1, date2);
        playerCity.updated_timestamp = new Date().toUTCString();
        
        worldCities[playerCityId] = playerCity;
        let vals = [playerCity.ownerID, playerCity.population, playerCity.army, playerCity.farmers, playerCity.resource, playerCity.buildingPayloadData, playerCity.npcPayloadData, playerCity.slaves, playerCity.updated_timestamp];
        updateCityDB(vals);
        //worldCities[playerCityId].cityName = data.cityName;
      }
      await getCityNPCs(playerCity.npcPayloadData);

  //    console.log('playerCity! ',playerCity)
     // room = playerCityId;
      var player = {
        sid: socket.id,
        id: thisPlayerId,
        cityId: playerCityId,
        room: playerCityId,
        location: [],
        rotation: 0.0,
        speed: 0.0,
        verticalVelocity: 0.0,
        inputMove: [],
        isAttacking: false,
      }
      
      players[thisPlayerId] = player;
  ///    moveRoom(socket, socket.id, playerCityId);
      socket.join(playerCityId)
      socket.emit('BuildScene', worldCities[playerCityId]);
      var worldData  = {"cities": await getWorldData()};
      socket.emit('WorldData', worldData);
      
    //  socket.emit('FetchPlayerCityData', playerCityId);
    //  console.log(worldCities[playerCityId]);
      //console.log(players[thisPlayerId]);
      data = {serverTimeNow: serverTimeNow, lastDBUpdateTime: playerCity.updated_timestamp};
      
      socket.emit('WhosThere', data);
  });

  socket.on('ItsMe', function(data) {      
      var clients= io.sockets.adapter.rooms;
      data.clients = clients;
      console.log("It'sME Rooms", io.sockets.adapter.rooms);
      //console.log('player room ', players[thisPlayerId].room);
//      console.log(npcPlayers);
      console.log('npc count ',getNPCCount());

      socket.emit('resetPlayers');
      socket.emit('SetNPCControl', true);
      for(var playerId in players){ 
 //         console.log(playerId)
          if(playerId == thisPlayerId){
 //           console.log('me')
            //socket.to(players[thisPlayerId].room).emit('Spawn', players[thisPlayerId]);
              continue;
          }
          console.log(players[playerId].sid)
          console.log(players[playerId].room, ' spawn emit? ', players[thisPlayerId].room)
          if (players[playerId].room === players[thisPlayerId].room){
              io.to(players[playerId].sid).emit('SetNPCControl', false);
              io.to(players[playerId].sid).emit('Spawn', players[thisPlayerId]);
              socket.emit('Spawn', players[playerId]);
  //            console.log('Sending spawn to new player for id: ',players[playerId],' from ',players[thisPlayerId]);// playerId, player.name);
          } 
      }
//        socket.to(players[thisPlayerId].room).emit('Spawn', players[thisPlayerId]);
  /*    if(getNPCCount() === 0){
        console.log("Fetch NPC data!!", npcPlayers.length);
        socket.emit("GetNPCData");
      }
      if(NPCsUpToDate){
        for(var npcId in npcPlayers){ 
              socket.emit('spawnNPC', npcPlayers[npcId]);
      }
      }*/
      /*for (var npc in npcPlayers){
        console.log(npc);
        socket.emit('spawnNPC', npcPlayers[npc]);
      }*/
    //  io.emit('clients', {currentClients: currentClients, maxClients: maxClients, totalClients: totalClients});
    //  io.emit('rooms', {data: getRoomsClients()});        
  });

  //*******************PLAYERS MOVES */
  socket.on('MoveRig', function(data) {
    
    data.id = thisPlayerId;
    data.sid = socket.id;
    //console.log(data)
    if(players[thisPlayerId]){
      players[thisPlayerId].location = data.location;
      players[thisPlayerId].rotation = data.rotation;
      players[thisPlayerId].speed = data.speed;
      players[thisPlayerId].verticalVelocity = data.verticalVelocity;
      players[thisPlayerId].inputMove = data.inputMove;

      socket.to(players[thisPlayerId].room).emit('moveRig', data);
    }
    else{
      console.log('Moving player not found')
    }
    
    /*for(var playerId in players){
        if(playerId === thisPlayerId){
            continue;
        }
        if (players[playerId].room === players[thisPlayerId].room){
            io.to(players[playerId].sid).emit('moveRig', data);
        }
    }*/
    
  });

  socket.on('ChangeAnimState', function(data) {
    console.log("ChangeAnimState ", data);
    socket.to(players[thisPlayerId].room).emit('ChangeAnimState', {sid: socket.id, id: thisPlayerId, animState: data});
  });

  socket.on('DeployFood', function(data) {
    console.log("DeployFood ", data);
    socket.to(players[thisPlayerId].room).emit('DeployFood', data);
  });

//*****************NPC HANDLING */
  socket.on('NpcPositionsToServer', function(data) {
    console.log("NPC Positions ", data);
    AddNPCPosition(data);
    //io.emit('rooms', {data: getRoomsClients()});
  //  console.log(npcPlayers);
  });  
  
  socket.on('NpcPositionsOK', function(data) {
    console.log("NpcPositionsOK ", data);
    NPCsUpToDate = true;
    if(NPCsUpToDate){
      for(var npcId in npcPlayers){ 
            socket.emit('spawnNPC', npcPlayers[npcId]);
      }
    }
  });

  socket.on('MoveNPC', function(data) {
    var npcPlayer = npcPlayers[data.id]
    npcPlayers[data.id].location = data.location;
    npcPlayers[data.id].target = data.target;
    npcPlayers[data.id].workAnimState = data.workAnimState;
    //console.log("MoveNPC", data);
    //io.emit("MoveNPC", npcPlayer);
    io.to(players[thisPlayerId].room).emit('MoveNPC', npcPlayer);
  });


  

//*************** CITY AND BUILDINGS *********** 

  socket.on('UpdatePlayerCityData', function(data) {
    console.log('UpdatePlayerCityData', worldCities);
    worldCities[playerCityId] = defaultCity;//data;
    console.log(worldCities);
    socket.emit('BuildScene', data);
  });

  socket.on('ChangeRoom', async function(data) {
      var newRoom = data.newRoom;
      var oldRoom = players[thisPlayerId].room;
      await getCityNPCs(worldCities[newRoom].npcPayloadData);
      var cityOwner = players[thisPlayerId].cityId == newRoom;

      data.id = thisPlayerId;
      data.sid = socket.id;
      socket.broadcast.emit('leaveRoom', data);
      moveRoom(socket, players[thisPlayerId].room, newRoom);
      players[thisPlayerId].room = newRoom;
      players[thisPlayerId].isAttacking = data.isAttacking;
      socket.emit('changeRoom', data);
      if(data.wasControllingNPC){ 
        for(var playerId in players){
          if(playerId === thisPlayerId){
            continue;
          }
          if (players[playerId].room === oldRoom){
            socket.to(players[playerId].sid).emit('SetNPCControl', true);
            break;        
          }
        }
      }
  
      var firstInRoom = true;
      socket.emit('BuildScene', worldCities[newRoom]);
      for(var playerId in players){
          if(playerId === thisPlayerId){
            continue;
          }
          if (players[playerId].room === newRoom){
            firstInRoom = false;
              io.to(players[playerId].sid).emit('Spawn', players[thisPlayerId]);
              players[playerId].isAttacking = data.isAttacking;
              socket.emit('Spawn', players[playerId]);
              if(cityOwner){
               // io.to(players[playerId].sid).emit('SetNPCControl', false);
              }
          }
      }
      if(cityOwner || firstInRoom){
        socket.emit('SetNPCControl', true);
        socket.to(players[thisPlayerId].room).emit('SetNPCControl', false);
      }else{
        socket.emit('SetNPCControl', false);
      }
  });
  
  socket.on('UpdatePlayerCity', function(playerCity) {
    playerCity.updated_timestamp = new Date().toUTCString();
    let vals = [playerCity.ownerID, playerCity.population, playerCity.army, playerCity.farmers, playerCity.resource, playerCity.buildingPayloadData, playerCity.npcPayloadData, playerCity.slaves, playerCity.updated_timestamp];
    worldCities[playerCityId] = playerCity;
    updateCityDB(vals);
  });
  
  socket.on('disconnect', function() {
      if(players[thisPlayerId] != null){
        var playerCity = worldCities[players[thisPlayerId].cityId];
        console.log('Client disconnected');
        playerCity.updated_timestamp = new Date().toUTCString();
        let vals = [playerCity.ownerID, playerCity.population, playerCity.army, playerCity.farmers, playerCity.resource, playerCity.buildingPayloadData, playerCity.npcPayloadData, playerCity.slaves, playerCity.updated_timestamp];
        updateCityDB(vals);
        delete players[thisPlayerId];
      }
      socket.broadcast.emit('disconnected', {sid: socket.id, id: thisPlayerId});
  });
})

function getNPCCount(){
var npcCount=0;
for(var npc in npcPlayers){
    npcCount++;
}
return npcCount;
}

// Postgresql functions
const { Client } = require('pg');
const client = new Client({
  connectionString: process.env.DATABASE_URL || "postgres://*******",
  ssl: {
    rejectUnauthorized: false 
  }
});

var serverStartTime = new Date().toUTCString();
var psqlBackup = true;

async function dbApp() {
  await client.connect();
  try {
    freeCityLocations = await getFreeCityLocations();
  } catch (error) {
    console.log(error)
  } finally {
    if(freeCityLocations.length < 10){
      randomCityLocation(100, true);
    }
  }
  // World cities .....
  try {
    await getCities();
    if(cityLocations.length < 20){ // Start building the world
      randomCitiesToDB(amountOfRandomCities, 'NPC_');
    }
  } catch (error) {
    console.log(error)
  } finally {
    serverReady = true;
    serverStatusToDB([serverStartTime, worldSizeMultiplier]);
  }
}  

async function getCity(vals){
  let city;
  let sql = `SELECT * FROM public."CitiesInWorld" WHERE "ownerID" = $1`;
  var res = await client.query(sql, [vals[0]]);
  for(let row of res.rows) {
    row.buildingPayloadData = JSON.parse(row.buildingPayloadData.toString());
    row.worldLocation = JSON.parse(row.worldLocation.toString());
    row.npcPayloadData = JSON.parse(row.npcPayloadData.toString());
    city = row;
  }
  return city ? city : false;
}

  async function getWorldData(){
    let sql = `SELECT "ownerID", population, "cityName", "worldLocation" FROM public."CitiesInWorld"`;
    var values = [];
    var res = await client.query(sql);
    for(let row of res.rows) {
      row.worldLocation = JSON.parse(row.worldLocation.toString());
      values.push(row);
    };
    return values;
  }

  async function getCities(){
    let sql = `SELECT * FROM public."CitiesInWorld"`;
    var res = await client.query(sql);
    for(let row of res.rows) {
      row.buildingPayloadData = JSON.parse(row.buildingPayloadData.toString());
      row.worldLocation = JSON.parse(row.worldLocation.toString());
      row.npcPayloadData = JSON.parse(row.npcPayloadData.toString());  
      worldCities[row.ownerID] = row;
      cityLocations.push(row.worldLocation);
    };
  }

  function updateCityDB(vals){
    vals[5] = Buffer.from(JSON.stringify(vals[5]))// buildingpayloaddata to binary    
    vals[6] = Buffer.from(JSON.stringify(vals[6]))// npcPayload to binary
    let sql = `UPDATE public."CitiesInWorld" SET population = $2, army = $3, farmers = $4, resource = $5,
     "buildingPayloadData" = $6, "npcPayloadData" = $7, slaves = $8, updated_timestamp = $9 WHERE "ownerID" = $1`;
    client.query(sql, vals, (err, res) => {
      if (err) throw err;
      console.log(`A city has been updated with new resource ${res.rows}`);      
    });
  } 

  async function addCityToDB(vals){
    vals[5] = Buffer.from(JSON.stringify(vals[5]))// buildingpayloaddata to binary
    vals[7] = Buffer.from(JSON.stringify(vals[7]))// worldLocation to binary
    vals[8] = Buffer.from(JSON.stringify(vals[8]))// npcPayload to binary
    let found = `SELECT * FROM public."CitiesInWorld" WHERE "ownerID" = $1`;
    client.query(found, [vals[0]], (err, res) => {
      if (err) throw err;   
      if (res.rows[0]) {
        console.log('city exists', res.rows[0]);
      }else{
        let sql = `INSERT INTO public."CitiesInWorld" VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $11, $10)`;
        client.query(sql, vals, (err, res) => {
          if (err) throw err;
          console.log(`A city has been inserted with rowid ${res.rows}`);
          cityLocations.push(JSON.parse(vals[7].toString()));
        });
      };     
    });
  }

  function updateDB(vals){
    let sql = `UPDATE public.freecityLocations SET inuse = $1 WHERE freelocations = $2`;
    client.query(sql, vals, (err, res) => {
      if (err) throw err;
      console.log(`A row has been updated with inuse true ${res.rows}`);      
    });
  } 

  async function getFreeCityLocations(){
    var freelocs = [];
    let sql = 'SELECT freelocations FROM freecityLocations WHERE inuse = false';
    var res = await client.query(sql);
    for(let row of res.rows) {
      row.freelocations = JSON.parse(row.freelocations.toString());
      freelocs.push(row.freelocations);
    };
    return freelocs;
  }

  function freeCityLocationsToDB(vals){
    vals[0] = Buffer.from(JSON.stringify(vals[0]))// buildingpayloaddata to binary  
    let sql2 = `INSERT INTO public.freecitylocations(inuse, freeLocations) VALUES ($2, $1);`;
    client.query(sql2, vals, (err, res) => {
      if (err) throw err;
    })
  }

  function serverStatusToDB(vals){
    let sql2 = `INSERT INTO public.server_stats(start_time, world_size_multiplier) VALUES ($1, $2);`;
    client.query(sql2, vals, (err, res) => {
      if (err) throw err;
    })
  }
  async function getServerStatusFromDB(){
    let sql = 'SELECT max(world_size_multiplier) FROM public.server_stats';
    var res = await client.query(sql);
    for(let row of res.rows) {
      console.log(row)
      worldSizeMultiplier = row.max;
    };
  }

  async function getServerConfigFromDB(){
    let sql = 'SELECT * FROM public.server_config WHERE id=1';
    var res = await client.query(sql);
    for(let row of res.rows) {
      console.log(row)
      amountOfRandomCities = row.random_cities_amount;
      serverVersion = row.server_version;
      defaultCity.army = row.def_army;
      defaultCity.farmers = row.def_farmer;
      defaultCity.slaves = row.def_slave;
    };
  }

dbApp();
