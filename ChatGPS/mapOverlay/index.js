'use strict';

let canvas, drawCtx;

class Point {
  constructor(x, y) {
    this.x = x;
    this.y = y;
  }
}

class Size {
  constructor(width, height) {
    this.width = width;
    this.height = height;
  }
}

class CoordTranslator {
  // x1 = aw * x + bw
  // y1 = ah * x + bh

  aw; bw; ah; bh

  getAB(x00, x01, x10, x11) {
    const a = (x00 - x01) / (x10 - x11);
    return [a, x00 - a * x10];
  }

  constructor(mapPoint1, picPoint1, mapPoint2, picPoint2) {
    [this.aw, this.bw] = this.getAB(picPoint1.x, picPoint2.x, mapPoint1.x, mapPoint2.x);
    [this.ah, this.bh] = this.getAB(picPoint1.y, picPoint2.y, mapPoint1.y, mapPoint2.y);
  }

  translatePoint(p) {
    return new Point(...this.translate(p.x, p.y));
  }

  translate(x, y) {
    return [this.aw * x + this.bw, this.ah * y + this.bh]
  }
}

class Marker {
  constructor(src) {
    this.image = new Image();
    this.image.src = src;
    this.x = this.y = 1000000;
    this.direction = null;
    this.enabled = false;
    this.hasImage = false;
    this.image.onload = () => { this.hasImage = true; }
  }

  setPosition(x, y) {
    this.x = Number(x);
    this.y = Number(y);
  }

  setEnabled(value) {
    this.enabled = value;
  }

  draw(ctx, x, y, w) {
    if (this.enabled && this.hasImage) {
      const rotated = typeof(this.direction) === "number";
      const h = this.image.height / this.image.width * w;

      if (rotated) {
        ctx.save();
        ctx.translate(x, y);
        ctx.rotate(this.direction);
        ctx.drawImage(this.image, - w / 2, - h / 2, w, h);
        ctx.restore();
      } else {
        ctx.drawImage(this.image, x - w / 2, y - h, w, h);
      }
    }
  }
}

class MapImage {
  constructor(ctx) {
    this.x = 0;
    this.y = 0;
    this.zoom = 500;
    this.ctx = ctx;
    this.image = new Image();
    this.hasImage = false;
    this.timeouts = [];
    this.playerMarker = new Marker('player-arrow.svg');
    this.destinationMarker = new Marker('destination-marker.svg');
	  this.playerArrow = new Marker('player-arrow.svg');
  }
  
  genTile(i, j, srcSize, dstSize) {
    const cvs = new OffscreenCanvas(dstSize, dstSize);
    const ctx = cvs.getContext('2d');
    ctx.drawImage(this.image, i * srcSize, j * srcSize, srcSize, srcSize, 0, 0, dstSize, dstSize);
    return cvs.transferToImageBitmap();
  }

  allocateTiles(srcSize) {
    if (!this.hasImage) {
      return [];
    }

    const tiles = [];

    for (let i = 0; i < this.image.naturalWidth / srcSize; ++i) {
      tiles.push([]);
      for (let j = 0; j < this.image.naturalHeight / srcSize; ++j) {
        tiles[i].push(undefined);
      }
    }

    return tiles;
  }

  genTilesBg(tiles, i, j, srcSize, dstSize) {
    let timeout = 30;
    console.log('generated', i, j, srcSize);
    if (tiles[i][j] === undefined) {
      tiles[i][j] = this.genTile(i, j, srcSize, dstSize);
    } else {
      timeout = 0;
    }

    if (j < tiles[i].length - 1) {
      ++j;
    } else if (i < tiles.length - 1) {
      ++i;
      j = 0;
    } else {
      return;
    }

    this.timeouts.push(setTimeout(() => this.genTilesBg(tiles, i, j, srcSize, dstSize), timeout));
  }

  genTiles(srcSize, dstSize) {
    const tiles = this.allocateTiles(srcSize);
    for (let i = 0; i < tiles.length; ++i) {
      for (let j = 0; j < tiles[i].length; ++j) {
        tiles[i][j] = this.genTile(i, j, srcSize, dstSize);
      }
    }
  }
  
  genAllTiles() {
    this.tiles250  = this.allocateTiles(250);
    this.tiles500  = this.allocateTiles(500);
    this.tiles1000 = this.allocateTiles(1000);
    this.tiles2000 = this.allocateTiles(2000);

    this.timeouts.push(setTimeout(() => { 
      this.genTilesBg(this.tiles2000, 0, 0, 2000, 250);
      this.genTilesBg(this.tiles1000, 0, 0, 1000, 250);
      this.genTilesBg(this.tiles500, 0, 0, 500, 250);
      this.genTilesBg(this.tiles250, 0, 0, 250, 250);
    }, 10000));
  }

  setPosition(x, y) {
    this.x = Number(x);
    this.y = Number(y);
  }
  
  setZoom(zoom) {
    this.zoom = Number(zoom);
  }

  setPlayerMarker(x, y, direction) {
    this.playerMarker.x = x;
    this.playerMarker.y = y;
    this.playerMarker.direction = direction;
    this.playerMarker.setEnabled(true);
    this.playerArrow.setEnabled(false);
  }
  
  setPlayerArrow(x, y, direction) {
    this.playerArrow.x = x;
	  this.playerArrow.y = y;
	  this.playerArrow.direction = direction;
	  this.playerMarker.setEnabled(false);
	  this.playerArrow.setEnabled(true);
  }

  setDestinationMarker(x, y) {
    this.destinationMarker.x = x;
    this.destinationMarker.y = y;
    this.destinationMarker.setEnabled(true);
  }

  disableMarkers() {
    this.playerMarker.setEnabled(false);
    this.destinationMarker.setEnabled(false);
	this.playerArrow.setEnabled(false);
  }

  setSrc(src, coordTranslator)
  {
    this.image.src = src;
    this.coordTranslator = coordTranslator;
    this.tiles250 = this.tiles500 = this.tiles1000 = this.tiles2000 = [];
    this.timeouts.forEach(t => window.clearTimeout(t));
    this.timeouts = [];
    this.image.onload = () => { this.hasImage = true; this.genAllTiles(); drawCanvas(); }
  }

  drawMarker(marker, cvsWidth, cvsHeight, dw = 1) {
    if (marker.enabled) {
      let [wzoom, hzoom] = [this.zoom, this.zoom]
      if (cvsWidth < cvsHeight)
      {
        hzoom = wzoom * cvsHeight / cvsWidth;
      } else {
        wzoom = hzoom * cvsWidth / cvsHeight;
      }

      const dx = (marker.x - this.x) / wzoom * cvsWidth;
      const dy = (marker.y - this.y) / hzoom * cvsHeight;
      marker.draw(this.ctx, cvsWidth / 2 + dx, cvsHeight / 2 - dy, Math.min(cvsWidth, cvsHeight) / 8 / dw);
    }
  }

  drawTiles(tiles, l, r, u, d, w, h) {
    w = w / (r - l);
    h = h / (d - u);

    const ts = [];

    for (let i = Math.max(Math.floor(l), 0); i < Math.min(r, tiles.length); ++i) {
      for (let j = Math.max(Math.floor(u), 0); j < Math.min(d, tiles[i].length); ++j) {
        if (tiles[i][j] === undefined) {
          return false;
        }

        ts.push({tile: tiles[i][j], x: w * (i - l), y: h * (j - u), w: w, h: h})
      }
    }

    ts.forEach(t => this.ctx.drawImage(t.tile, t.x, t.y, t.w, t.h));
    return true;
  }
  
  draw(cvsWidth = this.ctx.canvas.width, cvsHeight = this.ctx.canvas.height) {
    if (this.hasImage) {
      const [x, y] = this.coordTranslator.translate(this.x, this.y);
      
      let [w, h] = this.coordTranslator.translate(this.zoom, this.zoom);
      const [w0, h0] = this.coordTranslator.translate(0, 0);
      w = Math.abs(w - w0);
      h = Math.abs(h - h0);
      
      if (cvsWidth < cvsHeight)
      {
        h = w * cvsHeight / cvsWidth;
      } else {
        w = h * cvsWidth / cvsHeight;
      }
      
      const [l, r, u, d] = [(x - w / 2), (x + w / 2), (y - h / 2), (y + h / 2)]

      let tiles, denominator;

      if (w / cvsWidth < 2 && h / cvsHeight < 2) {
        tiles = this.tiles250;
        denominator = 250;
      } else if (w / cvsWidth < 4 && h / cvsHeight < 4) {
        tiles = this.tiles500;
        denominator = 500;
      } else if (w / cvsWidth < 8 && h / cvsHeight < 8) {
        tiles = this.tiles1000;
        denominator = 1000;
      } else {
        tiles = this.tiles2000;
        denominator = 2000;
      }

      if (!this.drawTiles(tiles, l / denominator, r / denominator, u / denominator, d / denominator, cvsWidth, cvsHeight)) {
        this.ctx.drawImage(this.image, l, u, w, h, 0, 0, cvsWidth, cvsHeight);
        console.log('tile not loaded, fallback to large image');
      }
      
      this.drawMarker(this.destinationMarker, cvsWidth, cvsHeight);
	    this.drawMarker(this.playerArrow, cvsWidth, cvsHeight, 1.5);
      this.drawMarker(this.playerMarker, cvsWidth, cvsHeight, 2.0);
    }
  }
}

class HelpText {
  constructor(ctx) {
    this.text = '';
    this.enabled = false;
    this.ctx = ctx;
  }

  setText(text, time) {
    this.text = text;
    this.enabled = true;
    if (typeof this.timeoutID === "number") {
      window.clearTimeout(this.timeoutID);
      this.timeoutID = null;
    }
    if (time > 0)
    {
      this.timeoutID = window.setTimeout(() => { this.text = ''; this.enabled = false; }, time);
    }
  }

  draw(cvsWidth = this.ctx.canvas.width, cvsHeight = this.ctx.canvas.height) {
    if (this.enabled) {
      this.ctx.font = 'bold ' +  cvsWidth / 9 + "px Verdana";
      
      this.ctx.textAlign = "center";
      
      let dy = 0;
      let w = 0;
      let split = this.text.split('\n');
      split.forEach(
        e => {
          dy += cvsWidth / 7;
          w = Math.max(w, this.ctx.measureText(e).width);
        }
      );
      
      this.ctx.beginPath();
      this.ctx.fillStyle = "white";
      this.ctx.rect(cvsWidth / 2 - w * 0.55, cvsHeight * 0.8 - cvsWidth / 8, w * 1.1, dy);
      this.ctx.fill();
      this.ctx.fillStyle = "#cc0d0a";
      
      dy = 0;
      split.forEach(
        e => {
          this.ctx.fillText(e, cvsWidth / 2, cvsHeight * 0.8 + dy, cvsWidth * 0.9);
          dy += cvsWidth / 7;
        });
    }
  }
}

let socket;
let mapImage;
let helpText;

let pictureImage;
let pictureEnabled = false;

function init() {
  initCanvas();
  initSocket();

  window.onresize = () => { resizeCanvas(); drawCanvas(); }
}

init();

function showWaitingImage() {
  mapImage.setPosition(0, -1000);
  mapImage.setZoom(7000);
  mapImage.disableMarkers();
  helpText.setText('Waiting for\nthe game', 0, () => {});
  pictureEnabled = false;
  drawCanvas();
}

function initSocket()
{
  socket = new WebSocket('ws://localhost:9873');
  
  showWaitingImage();

  socket.onclose = () => {
    showWaitingImage();
    console.log('socket closed; reconnecting'); 
    window.setTimeout(() => initSocket(), 500);
  }
  socket.onerror = () => { console.log('error in socket'); }
  socket.onopen = () => { console.log('connected to socket'); helpText.setText('Connected', 5000, true); }
  socket.onmessage = event => { 
    const message = JSON.parse(event.data);
    //console.log(message);
    
    switch (message.type) {
      case 'playerLocation':
        if (mapImage.destinationMarker.enabled) {
          const x = (message.x + mapImage.destinationMarker.x) / 2;
          const y = (message.y + mapImage.destinationMarker.y) / 2;
          mapImage.setPosition(x, y);

          const dx = Math.abs(message.x - mapImage.destinationMarker.x);
          const dy = Math.abs(message.y - mapImage.destinationMarker.y);
          mapImage.setZoom(Math.max(dx, dy) * 1.2 + 150);
        } else {
          mapImage.setPosition(message.x, message.y);
          mapImage.setZoom(2000);
        }

        if (message.small) {
          mapImage.setPlayerMarker(message.x, message.y, message.direction);
        } else {
          mapImage.setPlayerArrow(message.x, message.y, message.direction);
        }
        break;
      case 'destinationLocation':
        mapImage.setDestinationMarker(message.x, message.y);
        break;
      case 'clearDestination':
        mapImage.destinationMarker.setEnabled(false);
        break;
      case 'message':
        helpText.setText(message.message, message.time, true);
        break;
      case 'picture':
        if (message.filepath !== '' && (!pictureEnabled || pictureImage.src.indexOf(message.filepath.replaceAll('\\', '/')) === -1)) {
          pictureImage.src = message.filepath;
          pictureImage.onload = () => { pictureEnabled = true; }
        } else if (message.filepath === '') {
          pictureEnabled = false;
        }
        break;
      case 'mapInfo':
        mapImage.setSrc(message.filepath, 
          new CoordTranslator(
            new Point(message.mapx0, message.mapy0),
            new Point(message.picx0, message.picy0),
            new Point(message.mapx1, message.mapy1),
            new Point(message.picx1, message.picy1)), false);
        break;
    }

    if (Date.now() - message.timestamp <= 200) {
      drawCanvas();
    } else {
      console.log('Dropped frame');
    }
  }
}

function initCanvas()
{
  canvas = document.getElementById("canvas");
  drawCtx = canvas.getContext("2d");
  mapImage = new MapImage(drawCtx);
  mapImage.setSrc(//'gtav-map.svg',
    '../maps/gtav-map-satellite.jpg',
    //new CoordTranslator(new Point(0, 0), new Point(4015.07959, 8286.018), new Point(-4015.07959, 8286.018), new Point(0, 0)),
    new CoordTranslator(new Point(427, -1451), new Point(4037, 6483), new Point(-2845, 3342), new Point(1882, 3326)),
    true);
  helpText = new HelpText(drawCtx);
  pictureImage = new Image();
  resizeCanvas();
  drawCanvas();
}

function resizeCanvas() {
  canvas.width = Math.floor(canvas.clientWidth * window.devicePixelRatio);
  canvas.height = Math.floor(canvas.clientHeight * window.devicePixelRatio);
}

function drawCanvas() {
  drawCtx.clearRect(0, 0, canvas.width, canvas.height);
  if (!pictureEnabled) {
    mapImage.draw();
    helpText.draw();
  } else {
    let picWidth = canvas.width, picHeight = pictureImage.height / pictureImage.width * picWidth;

    const top = canvas.height - picHeight;
    mapImage.draw(picWidth, top);
    helpText.draw(picWidth, top);
    drawCtx.drawImage(pictureImage, 0, top, picWidth, picHeight);
  }
}