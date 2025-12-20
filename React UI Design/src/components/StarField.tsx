import React, { useEffect, useRef } from 'react';
type Star = {
  x: number;
  y: number;
  z: number;
  size: number;
  opacity: number;
  speed: number;
};
export function StarField() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const starsRef = useRef<Star[]>([]);
  const mouseRef = useRef({
    x: 0,
    y: 0
  });
  const animationRef = useRef<number>();
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    const resizeCanvas = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    const createStars = () => {
      const stars: Star[] = [];
      const numStars = 200;
      for (let i = 0; i < numStars; i++) {
        stars.push({
          x: Math.random() * canvas.width,
          y: Math.random() * canvas.height,
          z: Math.random() * 3 + 1,
          size: Math.random() * 2 + 0.5,
          opacity: Math.random() * 0.8 + 0.2,
          speed: Math.random() * 0.3 + 0.1
        });
      }
      // Add some larger, brighter stars
      for (let i = 0; i < 20; i++) {
        stars.push({
          x: Math.random() * canvas.width,
          y: Math.random() * canvas.height,
          z: Math.random() * 2 + 2,
          size: Math.random() * 3 + 2,
          opacity: Math.random() * 0.5 + 0.5,
          speed: Math.random() * 0.2 + 0.05
        });
      }
      starsRef.current = stars;
    };
    const drawStar = (star: Star, parallaxX: number, parallaxY: number) => {
      const x = star.x + parallaxX * star.z * 0.02;
      const y = star.y + parallaxY * star.z * 0.02;
      // Create gradient for star glow
      const gradient = ctx.createRadialGradient(x, y, 0, x, y, star.size * 2);
      if (star.size > 2) {
        // Larger stars get colored glow
        const hue = Math.random() > 0.5 ? 240 : 180; // Purple or cyan
        gradient.addColorStop(0, `hsla(${hue}, 80%, 80%, ${star.opacity})`);
        gradient.addColorStop(0.5, `hsla(${hue}, 60%, 60%, ${star.opacity * 0.5})`);
        gradient.addColorStop(1, 'transparent');
      } else {
        gradient.addColorStop(0, `rgba(248, 250, 252, ${star.opacity})`);
        gradient.addColorStop(0.5, `rgba(248, 250, 252, ${star.opacity * 0.3})`);
        gradient.addColorStop(1, 'transparent');
      }
      ctx.beginPath();
      ctx.arc(x, y, star.size * 2, 0, Math.PI * 2);
      ctx.fillStyle = gradient;
      ctx.fill();
      // Core of the star
      ctx.beginPath();
      ctx.arc(x, y, star.size * 0.5, 0, Math.PI * 2);
      ctx.fillStyle = `rgba(255, 255, 255, ${star.opacity})`;
      ctx.fill();
    };
    const drawNebula = () => {
      // Subtle nebula clouds
      const gradient1 = ctx.createRadialGradient(canvas.width * 0.2, canvas.height * 0.3, 0, canvas.width * 0.2, canvas.height * 0.3, canvas.width * 0.4);
      gradient1.addColorStop(0, 'rgba(99, 102, 241, 0.03)');
      gradient1.addColorStop(0.5, 'rgba(139, 92, 246, 0.02)');
      gradient1.addColorStop(1, 'transparent');
      ctx.fillStyle = gradient1;
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      const gradient2 = ctx.createRadialGradient(canvas.width * 0.8, canvas.height * 0.7, 0, canvas.width * 0.8, canvas.height * 0.7, canvas.width * 0.5);
      gradient2.addColorStop(0, 'rgba(6, 182, 212, 0.02)');
      gradient2.addColorStop(0.5, 'rgba(99, 102, 241, 0.01)');
      gradient2.addColorStop(1, 'transparent');
      ctx.fillStyle = gradient2;
      ctx.fillRect(0, 0, canvas.width, canvas.height);
    };
    const animate = () => {
      ctx.fillStyle = '#0a0e27';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      drawNebula();
      const centerX = canvas.width / 2;
      const centerY = canvas.height / 2;
      const parallaxX = (mouseRef.current.x - centerX) * 0.5;
      const parallaxY = (mouseRef.current.y - centerY) * 0.5;
      starsRef.current.forEach(star => {
        // Slow drift
        star.y += star.speed;
        star.x += star.speed * 0.2;
        // Wrap around
        if (star.y > canvas.height + 10) {
          star.y = -10;
          star.x = Math.random() * canvas.width;
        }
        if (star.x > canvas.width + 10) {
          star.x = -10;
        }
        // Twinkle effect
        star.opacity += (Math.random() - 0.5) * 0.02;
        star.opacity = Math.max(0.2, Math.min(1, star.opacity));
        drawStar(star, parallaxX, parallaxY);
      });
      animationRef.current = requestAnimationFrame(animate);
    };
    const handleMouseMove = (e: MouseEvent) => {
      mouseRef.current = {
        x: e.clientX,
        y: e.clientY
      };
    };
    resizeCanvas();
    createStars();
    animate();
    window.addEventListener('resize', () => {
      resizeCanvas();
      createStars();
    });
    window.addEventListener('mousemove', handleMouseMove);
    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
      window.removeEventListener('resize', resizeCanvas);
      window.removeEventListener('mousemove', handleMouseMove);
    };
  }, []);
  return <canvas ref={canvasRef} className="fixed inset-0 pointer-events-none" style={{
    zIndex: 0
  }} />;
}