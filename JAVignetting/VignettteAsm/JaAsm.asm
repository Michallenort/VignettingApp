; rcx - pixelArray
; rdx - index
; r8 - length
; r9 - centerX
; rsp + 40 - centerY
; rsp + 48 - radious
; rsp + 56 - gradientArea

.data
	const dd 4
	const2 dd 100

.code
Vignette proc
	;obliczenie warto�ci indeksu wiersza i zapisanie jej w rejestrze xmm0
	movd xmm0, rdx
	movd xmm1, r8
	cvtdq2ps xmm0, xmm0
	cvtdq2ps xmm1, xmm1
	divps xmm0, xmm1

	;ustawienie indeksu pocz�tku p�tli w rejestrze r10 oraz
	;indeksu ko�ca p�tli w rejestrze r11
	mov r10, rdx
	mov r11, rdx
	add r11, r8

mainloop:
	;obliczenie warto�ci aktualnego indeksu kolumny
	mov r12, r10
	sub r12, rdx
	movd xmm1, r12
	cvtdq2ps xmm1, xmm1
	movd xmm2, [const]
	cvtdq2ps xmm2, xmm2
	divps xmm1, xmm2

	;obliczenie odleg�o�ci aktualnego piksela od �rodka p�tli na osi x
	movd xmm5, r9
	cvtdq2ps xmm5, xmm5
	subps xmm1, xmm5

	;obliczenie odleg�o�ci aktualnego piksela od �rodka p�tli na osi y
	mov r13, [rsp + 40]
	movd xmm5, r13
	cvtdq2ps xmm5, xmm5
	movq xmm2, xmm0
	subps xmm2, xmm5

	;podniesienie do kwadratu odleg�o�ci na osi x i y
	mulps xmm1, xmm1
	mulps xmm2, xmm2
	
	;obliczenie odleg�o�ci piksela od �rodka
	addps xmm1, xmm2
	sqrtps xmm1, xmm1
	cvtss2si r14, xmm1

	;sprawdzenie czy odleg�o�� jest mniejsza od promienia winiety
	;je�li tak to przechodzimy do nast�pnego piksela
	mov r13, [rsp + 48]
	cmp r14, r13
	jl nextPixel

	;sprawdzenie czy odleg�o�� jest wi�ksza od sumy promienia i wielko��i
	;obszaru gradientowanego
	;je�li nie to przechodzimy do obliczania warto�ci wagi
	add r13, [rsp + 56]
	cmp r13, r14
	jl substractMaxValue

	;odczytanie zawarto�ci promienia
	mov r13, [rsp + 48]
	movd xmm2, r13
	cvtdq2ps xmm2, xmm2

	; odczytanie wielko��i obszaru gradientowanego
	mov r13, [rsp + 56]
	movd xmm3, r13
	cvtdq2ps xmm3, xmm3

	movd xmm4, [const2]
	cvtdq2ps xmm4, xmm4

	;obliczenie wagi
	subps xmm1, xmm2
	mulps xmm1, xmm4
	divps xmm1, xmm3
	cvtps2dq xmm1, xmm1
	vpbroadcastd xmm1, xmm1

	;pobranie warto�ci piksela do rejestru xmm2
	pmovzxbd xmm2, [rcx + r10]
	;odj�cie od warto�ci r, g, b wagi
	psubd xmm2, xmm1
	jmp blue
	
	
substractMaxValue:	
	;pobranie warto�ci piksela do rejestru xmm2
	pmovzxbd xmm2, [rcx + r10]
	movd xmm1, [const2]
	vpbroadcastd xmm1, xmm1
	;;odj�cie od warto�ci r, g, b wagi r�wnej 100
	psubd xmm2, xmm1

	;sprawdzenie czy warto�ci r,g,b nie s� mniejsze od 0
	;je�li tak jest to wpisz w ich miejsce do tablicy warto�� 0
	;w miejsce warto�ci alfa wpisana zostanie warto�� 255
blue:
	movd eax, xmm2
	cmp eax, 0
	jl blueZero
green:
	mov [rcx + r10], al
	psrldq xmm2, 4
	movd eax, xmm2
	cmp eax, 0
	jl greenZero
red:
	mov [rcx + r10 + 1], al
	psrldq xmm2, 4
	movd eax, xmm2
	cmp eax, 0
	jl redZero
alpha: 
	mov [rcx + r10 + 2], al
	mov al, 255
	mov [rcx + r10 + 3], al 
	jmp nextPixel

blueZero:
	mov al, 0
	jmp green
greenZero:
	mov al, 0
	jmp red
redZero:
	mov al, 0
	jmp alpha
	
nextPixel:
	;zwi�kszenie indeksu p�tli o 4
	add r10, 4 
	;sprawdzenie czy indeks jest w zakresie tablicy
	cmp r10, r11
	;skok do nast�pnego piksela
	jl mainloop
	
	ret
Vignette endp
end