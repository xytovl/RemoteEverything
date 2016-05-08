KSPDIR  ?= ${HOME}/.local/share/Steam/SteamApps/common/Kerbal\ Space\ Program
MANAGED := ${KSPDIR}/KSP_Data/Managed/

SOURCEFILES := $(wildcard RemoteEverything/*.cs)\
	$(wildcard RemoteEverything/HttpServer/*.cs)\
	$(wildcard RemoteEverything/Json/*.cs)

RESGEN2 := resgen2
GMCS    ?= mcs
GIT     := git
ZIP     := zip

VERSION_MAJOR := 0
VERSION_MINOR := 2
VERSION_PATCH := 0

VERSION := ${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_PATCH}

ifeq ($(debug),1)
	DEBUG = -debug -define:DEBUG
endif

all: build/RemoteEverything.dll

info:
	@echo "== RemoteEverything Build Information =="
	@echo "  resgen2: ${RESGEN2}"
	@echo "  gmcs:    ${GMCS}"
	@echo "  git:     ${GIT}"
	@echo "  zip:     ${ZIP}"
	@echo "  KSP Data: ${KSPDIR}"
	@echo "================================"

build/%.dll: ${SOURCEFILES}
	mkdir -p build
	${GMCS} -t:library -lib:${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine,KSPUtil,UnityEngine.UI \
		${DEBUG} \
		-out:$@ \
		${SOURCEFILES}


package: build/RemoteEverything.dll
	mkdir -p package/RemoteEverything/www
	cp RemoteEverything/img/* package/RemoteEverything/
	cp -r SampleWebUI/* package/RemoteEverything/www/
	cp $< package/RemoteEverything/

%.zip:
	cd package && ${ZIP} -9 -r ../$@ RemoteEverything

zip: package RemoteEverything-${VERSION}.zip

release: zip
	git commit -m "release v${VERSION}" Makefile
	git tag v${VERSION}

clean:
	@echo "Cleaning up build and package directories..."
	rm -rf build/ package/

install: package
	cp -r package/RemoteEverything ${KSPDIR}/GameData/

uninstall: info
	rm -rf ${KSPDIR}/GameData/RemoteEverything


.PHONY : all info build package zip clean install uninstall
