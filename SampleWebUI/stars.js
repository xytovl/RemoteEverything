// inspired by http://giacomofurlan.name/my-experiments/starry-sky-canvas
// license http://creativecommons.org/licenses/by/3.0/
document.addEventListener("DOMContentLoaded", function () {
	var canvas = document.createElement("canvas");
	canvas.style.position = "fixed";
	document.body.insertBefore(canvas, document.body.firstElementChild);
	window.addEventListener("resize", function() { drawStars(canvas);});

	drawStars(canvas);
});

var drawStars = function(canvas)
{
	if (canvas.width >= document.width && canvas.height >= document.height)
		return;
	canvas.width = Math.max(canvas.width, document.width);
	canvas.height = Math.max(canvas.height, document.height);
	var context = canvas.getContext('2d'),
		max_bright = 1,
		min_bright = .2;

	/* LOGICS */
	generate(canvas.width * canvas.height * 0.001, .5);
	
	/* FUNCTIONS */
	function generate(starsCount, opacity) {
		for(var i = 0; i < starsCount; i++) {
			var x = randomInt(2, canvas.offsetWidth-2),
				y = randomInt(2, canvas.offsetHeight-2);

			star(x, y, 2.5, opacity);
		}
	}

	function star(x, y, size, alpha) {
		var radius = Math.random() * size;

		gradient = context.createRadialGradient(x, y, 0, x + radius, y + radius, radius * 2);
		gradient.addColorStop(0, 'rgba(' + randomInt(100, 255) + ',' + randomInt(100, 255) + ','+ randomInt(100, 255) + ',' + alpha + ')');
		gradient.addColorStop(1, 'rgba(0, 0, 0, 0)');

		/* clear background pixels */
		context.beginPath();
		context.clearRect(x - radius - 1, y - radius - 1, radius * 2 + 2, radius * 2 + 2);
		context.closePath();

		/* draw star */
		context.beginPath();
		context.arc(x,y,radius,0,2*Math.PI);
		context.fillStyle = gradient;
		context.fill();

		return {
			'x': x,
			'y': y,
			'size': size,
			'alpha': alpha
		};
	}

	function randomInt(a, b) {
		return Math.floor(Math.random()*(b-a+1)+a);
	}

	function randomFloatAround(num) {
		var plusminus = randomInt(0, 1000) % 2,
			val = num;
		if(plusminus)
			val += 0.1;
		else
			val -= 0.1;
		return parseFloat(val.toFixed(1));
	}
};
