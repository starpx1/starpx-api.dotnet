const express = require('express');
const bodyParser = require('body-parser');

const app = express();
const PORT = 3000;

app.use(bodyParser.json());

// Mock data
const mockAccessToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';
const mockPlateSolveResult = {
  platesolveId: 'platesolve123',
  solved: true,
  skyCoordinate: {
    ra: 180.1,
    dec: 45.1
  }
};

// /authenticate endpoint
app.post('/authenticate', (req, res) => {
  const apiKey = req.header('apiKey');
  if (apiKey === 'valid-api-key') {
    res.json({
      apiVersion: '0.1.0',
      accessToken: mockAccessToken,
      expiresIn: 57600
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
  if (authHeader === `Bearer ${mockAccessToken}`) {
    res.json(mockPlateSolveResult);
  } else {
    res.status(401).json({
      message: 'Unauthorized - Invalid or missing access token'
    });
  }
});

// /platesolve/:platesolve_id endpoint
app.get('/platesolve/:platesolve_id', (req, res) => {
  const authHeader = req.header('Authorization');
  if (authHeader === `Bearer ${mockAccessToken}`) {
    if (req.params.platesolve_id === 'platesolve123') {
      res.json(mockPlateSolveResult);
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