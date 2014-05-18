KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
APIGAMEDATA	:= ${GAMEDATA}/KSPAPIExt
PLUGINDIR	:= ${APIGAMEDATA}/Plugins

TARGETS		:= KSPAPIExtensions.dll

API_FILES := \
    DuckTyping/AmbiguousTypesException.cs \
    DuckTyping/DelegateDuckCaster.cs \
    DuckTyping/DelegateDuckProxyType.cs \
    DuckTyping/DuckProxyType.cs \
    DuckTyping/DuckTyping.cs \
    DuckTyping/EnumStringDuckCaster.cs \
    DuckTyping/FromTypeTree.cs \
    DuckTyping/GenericParameterDuckCaster.cs \
    DuckTyping/IDuckCaster.cs \
    DuckTyping/IDuckProxy.cs \
    DuckTyping/InterfaceDuckCaster.cs \
    DuckTyping/InterfaceDuckProxyType.cs \
    DuckTyping/InterfaceStaticDuckCaster.cs \
    DuckTyping/IStaticDuckCaster.cs \
    DuckTyping/ToTypeFromTypeTree.cs \
    DuckTyping/TypeKey.cs \
    Utils/EngineWrapper.cs \
    Utils/MathUtils.cs \
    Utils/DebuggingUtils.cs \
    PartMessage/CommonMessages.cs \
    PartMessage/Attributes.cs \
    PartMessage/zzImplementation.cs \
    PartMessage/PartMessageService.cs \
    Utils/PhysicsDebugger.cs \
    Properties/AssemblyInfo.cs \
    Utils/KSPUtils.cs \
    Utils/SystemUtils.cs \
    Utils/UnityUtils.cs \
    Tweakables/UIPartActionChooseOption.cs \
    Tweakables/UIPartActionsExtended.cs \
    Tweakables/UIPartActionFloatEdit.cs \
    zzVersionChecker.cs \
	$e

RESGEN2	:= resgen2
GMCS	:= gmcs
GIT		:= git
TAR		:= tar
ZIP		:= zip

all: version ${TARGETS}

.PHONY: version
version:
	@../version-gen

info:
	@echo "Modular Fuel Tanks Build Information"
	@echo "    resgen2:  ${RESGEN2}"
	@echo "    gmcs:     ${GMCS}"
	@echo "    git:      ${GIT}"
	@echo "    tar:      ${TAR}"
	@echo "    zip:      ${ZIP}"
	@echo "    KSP Data: ${KSPDIR}"
	@echo "    Plugin:   ${PLUGINDIR}"

KSPAPIExtensions.dll: ${API_FILES}
	${GMCS} -unsafe -t:library -lib:${APIEXTDATA},${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-out:$@ $^

clean:
	rm -f ${TARGETS} AssemblyInfo.cs

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} ${PLUGINDIR}

.PHONY: all clean install
