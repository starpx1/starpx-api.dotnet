const express = require('express');
const bodyParser = require('body-parser');
const jwt = require('jsonwebtoken');
require('dotenv').config();
const app = express();
const PORT = 3000;
var number = 0;
var generalToken;
app.use(bodyParser.json());
let arr = [90, 180];
let arr2 = [45, 90];
function random(mn, mx) {
  return Math.random() * (mx - mn) + mn;
}

// Mock data
// const mockAccessToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';
// const mockPlateSolveResult = {
//   platesolveId: 'platesolve' + (int + 1),
//   solved: true,
//   skyCoordinate: {
//     ra: arr[(Math.floor(random(1, 2))) - 1],
//     dec: arr2[(Math.floor(random(1, 2))) - 1]
//   }
// };
var plateSolveResult;
function generateAccessToken(payload) {
  const secretKey = process.env.SECRET_KEY;

  return jwt.sign(payload, secretKey, { expiresIn: '1h' });

}
// /authenticate endpoint
app.post('/authenticate', (req, res) => {
  const apiKey = req.header('apiKey');
  if (apiKey === 'valid-api-key') {
    const payload = {
      user_id: 'user123',
      password: 'Ab@123456',
      scopes: ['read', 'write']
    };

    const accessToken = generateAccessToken(payload);
    generalToken=accessToken;
    res.json({
      apiVersion: '0.1.0',
      tokenType: 'Bearer',
      accessToken: accessToken,
      expiresIn: 3600
    });
  } else {
    res.status(401).json({
      message: 'Unauthorized - API key is invalid'
    });
  }
});

// /platesolve endpoint
app.post('/platesolve', (req, res) => {
  const authHeader = req.header('Authorization');
  // const token = authHeader.split(' ')[1];
  const token = generalToken;

  const decoded = jwt.verify(token, process.env.SECRET_KEY);
  if (token && decoded) {
    number=number+1
    plateSolveResult = {
      platesolveId: 'platesolve' + number,
      solved: false,
      skyCoordinate: {
        ra: arr[(Math.floor(random(1, 2))) - 1],
        dec: arr2[(Math.floor(random(1, 2))) - 1]
      }
    }
    res.json(plateSolveResult);
  } else {
    res.status(401).json({
      message: 'Unauthorized - Invalid or missing access token'
    });
  }
});

// /platesolve/:platesolve_id endpoint
app.get('/platesolve/:platesolve_id', (req, res) => {
  const authHeader = req.header('Authorization');
  // const token = authHeader.split(' ')[1];
  const token = generalToken;

  const decoded = jwt.verify(token, process.env.SECRET_KEY);
  if (token && decoded) {
    if (req.params.platesolve_id === 'platesolve' + number) {
      plateSolveResult.solved = true
      res.json(plateSolveResult);
    } else {
      res.status(400).json({
        errorMessage: 'Invalid platesolve ID'
      });
    }
  } else {
    res.status(401).json({
      message: 'Unauthorized - Invalid or missing access token'
    });
  }
});

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});