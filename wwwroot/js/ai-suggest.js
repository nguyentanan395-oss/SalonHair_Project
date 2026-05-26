﻿﻿import vision from "/vendor/mediapipe/vision_bundle.js";

const { FaceLandmarker, FilesetResolver, DrawingUtils } = vision;

const MODEL_URL = "/vendor/mediapipe/models/face_landmarker.task";
const WASM_URL = "/vendor/mediapipe/wasm";
const MODEL_LOAD_TIMEOUT_MS = 15000;
const UPDATE_COOLDOWN_MS = 1200;

const appRoot = document.getElementById("aiSuggestApp");

if (appRoot) {
  const dom = {
    fileInput: document.getElementById("faceImageInput"),
    selectButton: document.getElementById("selectImageButton"),
    analyzeButton: document.getElementById("analyzeFaceButton"),
    resetButton: document.getElementById("resetAnalysisButton"),
    startCameraButton: document.getElementById("startCameraButton"),
    stopCameraButton: document.getElementById("stopCameraButton"),
    genderSelect: document.getElementById("genderSelect"),
    ageGroupSelect: document.getElementById("ageGroupSelect"),
    profileHint: document.getElementById("profileHint"),
    video: document.getElementById("webcamVideo"),
    canvas: document.getElementById("facePreviewCanvas"),
    emptyState: document.getElementById("scanEmptyState"),
    uploadHint: document.getElementById("uploadHint"),
    status: document.getElementById("scanStatus"),
    metricShape: document.getElementById("metricShape"),
    metricConfidence: document.getElementById("metricConfidence"),
    metricRatio: document.getElementById("metricRatio"),
    results: document.getElementById("scanResults"),
    resultTitle: document.getElementById("resultTitle"),
    resultSummary: document.getElementById("resultSummary"),
    resultConfidence: document.getElementById("resultConfidence"),
    resultMeta: document.getElementById("resultMeta"),
    tips: document.getElementById("resultTips"),
    cards: document.getElementById("resultCards"),
    feedbackPanel: document.getElementById("feedbackPanel"),
    feedbackAcceptButton: document.getElementById("feedbackAcceptButton"),
    feedbackShapeButtons: Array.from(
      document.querySelectorAll(".feedback-shape-button"),
    ),
    feedbackStatus: document.getElementById("feedbackStatus"),
    exportFeedbackButton: document.getElementById("exportFeedbackButton"),
    importFeedbackButton: document.getElementById("importFeedbackButton"),
    importFeedbackInput: document.getElementById("feedbackImportInput"),
    trainModelButton: document.getElementById("trainModelButton"),
    datasetStatus: document.getElementById("datasetStatus"),
    batchFolderInput: document.getElementById("batchFolderInput"),
    selectBatchFolderButton: document.getElementById("selectBatchFolderButton"),
    skipBatchImageButton: document.getElementById("skipBatchImageButton"),
    stopBatchButton: document.getElementById("stopBatchButton"),
    batchStatus: document.getElementById("batchStatus"),
    batchProgress: document.getElementById("batchProgress"),
  };

  const state = {
    faceLandmarker: null,
    runningMode: "IMAGE",
    image: null,
    objectUrl: null,
    stream: null,
    isCameraRunning: false,
    rafId: 0,
    lastVideoTime: -1,
    lastSentShape: "",
    lastSentAt: 0,
    isSending: false,
    isSavingFeedback: false,
    lastAnalysis: null,
    lastMetrics: null,
    lastDetectionSource: "",
    lastLandmarksJson: "",
    queuedAnalysisRequest: null,
    runtimeModel: null,
    selectedGender: "",
    selectedAgeGroup: "",
    clientSessionId: getClientSessionId(),
    analysisSequence: 0,
    activeAnalysisToken: 0,
    batch: {
      active: false,
      files: [],
      index: 0,
      labeledCount: 0,
      skippedCount: 0,
      autoSkippedCount: 0,
      currentFileName: "",
      isBusy: false,
    },
  };

  const faceOvalIndices = [];

  for (const connection of FaceLandmarker.FACE_LANDMARKS_FACE_OVAL) {
    if (!faceOvalIndices.includes(connection.start)) {
      faceOvalIndices.push(connection.start);
    }

    if (!faceOvalIndices.includes(connection.end)) {
      faceOvalIndices.push(connection.end);
    }
  }

  bindEvents();
  initializeFeedbackButtonLabels();
  updateBatchUi();
  updateAnalysisControls();
  boot();

  async function boot() {
    setStatus(
      "Đang tải mô hình AI lần đầu. Nếu mạng chậm có thể mất 10-15 giây...",
      "loading",
    );

    try {
      const filesetResolver = await withTimeout(
        FilesetResolver.forVisionTasks(WASM_URL),
        MODEL_LOAD_TIMEOUT_MS,
        "Timeout while loading MediaPipe fileset.",
      );

      try {
        state.faceLandmarker = await withTimeout(
          FaceLandmarker.createFromOptions(filesetResolver, {
            baseOptions: {
              modelAssetPath: MODEL_URL,
              delegate: "GPU",
            },
            runningMode: "IMAGE",
            numFaces: 1,
          }),
          MODEL_LOAD_TIMEOUT_MS,
          "Timeout while loading GPU face landmarker.",
        );
      } catch {
        state.faceLandmarker = await withTimeout(
          FaceLandmarker.createFromOptions(filesetResolver, {
            baseOptions: {
              modelAssetPath: MODEL_URL,
              delegate: "CPU",
            },
            runningMode: "IMAGE",
            numFaces: 1,
          }),
          MODEL_LOAD_TIMEOUT_MS,
          "Timeout while loading CPU face landmarker.",
        );
      }

      setStatus(
        "AI đã sẵn sàng. Bạn có thể mở webcam trực tiếp hoặc tải ảnh chân dung.",
        "ready",
      );
      updateAnalysisControls();
      updateBatchUi();
      await loadRuntimeModel();
    } catch (error) {
      console.error(error);
      setStatus(
        "Không tải được mô hình AI. Hãy tải lại trang hoặc kiểm tra mạng/CDN.",
        "error",
      );
    }
  }

  function bindEvents() {
    dom.selectButton?.addEventListener("click", () => dom.fileInput?.click());
    dom.fileInput?.addEventListener("change", handleFileSelect);
    dom.analyzeButton?.addEventListener("click", analyzeImage);
    dom.resetButton?.addEventListener("click", resetAnalysis);
    dom.startCameraButton?.addEventListener("click", startCamera);
    dom.stopCameraButton?.addEventListener("click", stopCamera);
    dom.genderSelect?.addEventListener("change", handleProfileSelectionChange);
    dom.ageGroupSelect?.addEventListener(
      "change",
      handleProfileSelectionChange,
    );
    dom.feedbackAcceptButton?.addEventListener("click", () => {
      if (state.lastAnalysis) {
        saveFeedback(state.lastAnalysis.shape);
      }
    });
    dom.feedbackShapeButtons.forEach((button) => {
      button.addEventListener("click", () =>
        saveFeedback(button.dataset.shape || ""),
      );
    });
    dom.exportFeedbackButton?.addEventListener("click", exportDataset);
    dom.importFeedbackButton?.addEventListener("click", () =>
      dom.importFeedbackInput?.click(),
    );
    dom.importFeedbackInput?.addEventListener("change", importDataset);
    dom.trainModelButton?.addEventListener("click", trainModel);
    dom.selectBatchFolderButton?.addEventListener("click", () =>
      dom.batchFolderInput?.click(),
    );
    dom.batchFolderInput?.addEventListener("change", handleBatchFolderSelect);
    dom.skipBatchImageButton?.addEventListener("click", skipCurrentBatchImage);
    dom.stopBatchButton?.addEventListener("click", () => stopBatchSession());
    window.addEventListener("beforeunload", cleanupCamera);
    document.addEventListener("visibilitychange", () => {
      if (document.hidden) {
        cleanupCamera();
      }
    });
  }

  async function startCamera() {
    if (!state.faceLandmarker) {
      setStatus(
        "Mô hình AI chưa sẵn sàng. Hãy đợi thêm một chút rồi thử lại.",
        "error",
      );
      return;
    }

    const profile = getSelectedProfile();
    if (!profile.ready) {
      setStatus(
        "Webcam đã mở. Chọn giới tính và nhóm tuổi để AI gợi ý kiểu tóc chính xác hơn.",
        "loading",
      );
    }

    if (!navigator.mediaDevices?.getUserMedia) {
      setStatus("Trình duyệt này không hỗ trợ webcam realtime.", "error");
      return;
    }

    stopBatchSession({ silent: true });
    cleanupCamera(); // Dọn dẹp trực tiếp trước khi khởi tạo stream mới
    clearImageState();

    try {
      await ensureMode("VIDEO");

      const stream = await navigator.mediaDevices.getUserMedia({
        video: {
          facingMode: "user",
          width: { ideal: 1280 },
          height: { ideal: 720 },
        },
        audio: false,
      });

      state.stream = stream;
      state.isCameraRunning = true;
      state.lastVideoTime = -1;

      dom.video.srcObject = stream;
      dom.video.classList.remove("is-hidden");
      dom.video.classList.add("is-mirrored");
      dom.canvas.classList.add("is-mirrored");
      dom.emptyState.classList.add("d-none");
      if (dom.uploadHint)
        dom.uploadHint.textContent =
          "Webcam đang chạy realtime. Hãy giữ khuôn mặt chính diện.";

      dom.startCameraButton.disabled = true;
      dom.stopCameraButton.disabled = false;
      dom.analyzeButton.disabled = true;

      await dom.video.play();
      setStatus("Webcam đã mở. AI đang quét khuôn mặt...", "ready");

      if (state.rafId) {
        cancelAnimationFrame(state.rafId);
      }

      state.rafId = requestAnimationFrame(processVideoFrame);
    } catch (error) {
      console.error(error);
      cleanupCamera();
      dom.emptyState.classList.remove("d-none");
      setStatus(
        "Không mở được webcam. Hãy kiểm tra quyền camera của trình duyệt.",
        "error",
      );
    }
  }

  function stopCamera() {
    cleanupCamera();

    if (!state.image) {
      dom.video.classList.add("is-hidden");
      dom.emptyState.classList.remove("d-none");
      dom.uploadHint.textContent =
        "Bấm mở webcam để quét trực tiếp, hoặc tải ảnh chân dung một người nếu cần tư vấn thủ công.";
      resetMetrics();
      clearCanvas();
    }
  }

  function cleanupCamera() {
    state.isCameraRunning = false;

    if (state.rafId) {
      cancelAnimationFrame(state.rafId);
      state.rafId = 0;
    }

    if (state.stream) {
      for (const track of state.stream.getTracks()) {
        track.stop();
      }
      state.stream = null;
    }

    if (dom.video.srcObject) {
      dom.video.srcObject = null;
    }

    dom.video.classList.add("is-hidden");
    dom.video.classList.remove("is-mirrored");
    dom.canvas.classList.remove("is-mirrored");
    updateAnalysisControls();
    dom.stopCameraButton.disabled = true;
  }

  async function handleFileSelect(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    stopBatchSession({ silent: true });
    cleanupCamera();

    try {
      await loadFileIntoStage(file, {
        hintTextFactory: (image) =>
          `${file.name} | ${image.naturalWidth} x ${image.naturalHeight}`,
        enableAnalyzeButton: true,
      });
      updateAnalysisControls();
      setStatus(
        "Ảnh đã sẵn sàng. Bấm phân tích để nhận gợi ý kiểu tóc.",
        "ready",
      );
    } catch (error) {
      console.error(error);
      setStatus("Không đọc được ảnh này. Hãy thử file khác.", "error");
    }
    return;

    /* Legacy loader block kept unreachable after refactor.
        image.onload = () => {
            state.image = image;
            dom.canvas.classList.remove("is-mirrored");
            dom.emptyState.classList.add("d-none");
            dom.uploadHint.textContent = `${file.name} | ${image.naturalWidth} x ${image.naturalHeight}`;
            drawImageToCanvas();
            dom.analyzeButton.disabled = !state.faceLandmarker;
            setStatus("Ảnh đã sẵn sàng. Bấm phân tích để nhận gợi ý kiểu tóc.", "ready");
        };

        image.src = state.objectUrl;
        */
  }

  async function analyzeImage() {
    if (!state.faceLandmarker || !state.image) {
      setStatus("Bạn cần tải ảnh chân dung trước khi phân tích.", "error");
      return;
    }

    cleanupCamera();
    await ensureMode("IMAGE");
    setStatus("AI đang phân tích ảnh khuôn mặt...", "loading");

    try {
      const detection = state.faceLandmarker.detect(state.image);
      await handleDetectionResult(detection, "image");
    } catch (error) {
      console.error("Analysis Error:", error);
      setStatus(
        "Không thể phân tích ảnh này. Hãy thử ảnh sáng và rõ mặt hơn.",
        "error",
      );
    }
  }

  async function processVideoFrame() {
    if (!state.isCameraRunning || !state.faceLandmarker) {
      return;
    }

    if (dom.video.readyState < 2) {
      state.rafId = requestAnimationFrame(processVideoFrame);
      return;
    }

    resizeCanvasToVideo();

    if (dom.video.currentTime !== state.lastVideoTime) {
      const nowInMs = performance.now();

      try {
        const detection = state.faceLandmarker.detectForVideo(
          dom.video,
          nowInMs,
        );
        await handleDetectionResult(detection, "video");
        state.lastVideoTime = dom.video.currentTime;
      } catch (error) {
        console.error(error);
        setStatus(
          "AI bị gián đoạn khi đọc webcam. Hãy dừng rồi mở lại camera.",
          "error",
        );
        stopCamera();
        return;
      }
    }

    state.rafId = requestAnimationFrame(processVideoFrame);
  }

  async function handleDetectionResult(detection, source) {
    const faces = detection.faceLandmarks ?? [];

    if (faces.length === 0) {
      clearCanvas(source);
      clearFeedbackState();

      if (source === "video") {
        setStatus("Đang chờ khuôn mặt vào khung hình...", "loading");
      } else {
        setStatus(
          "Không nhận ra khuôn mặt trong ảnh. Hãy chọn ảnh chính diện và sáng hơn.",
          "error",
        );
      }

      resetMetrics();
      return;
    }

    if (faces.length > 1) {
      clearCanvas(source);
      clearFeedbackState();
      setStatus(
        "AI thấy nhiều hơn một khuôn mặt. Hãy giữ khung chỉ còn một khách.",
        "error",
      );
      resetMetrics();
      return null;
    }

    const landmarks = faces[0];
    drawOverlay(landmarks, source);

    const metrics = measureFace(landmarks);
    const analysis = classifyFace(metrics);
    state.lastAnalysis = analysis;
    state.lastMetrics = metrics;
    state.lastDetectionSource = source;
    state.lastLandmarksJson = serializeLandmarks(landmarks);
    const analysisToken = ++state.analysisSequence;
    state.activeAnalysisToken = analysisToken;
    renderQuickMetrics(analysis, metrics);
    dom.feedbackPanel?.classList.remove("d-none");
    setFeedbackStatus(buildFeedbackPrompt(analysis.shape));

    if (source === "video") {
      setStatus(
        "Webcam đang quét ổn định. AI sẽ cập nhật gợi ý khi dáng mặt đủ rõ.",
        "ready",
      );
    } else {
      setStatus(
        "Phân tích ảnh xong. Đang ghép gợi ý kiểu tóc phù hợp...",
        "ready",
      );
    }

    return maybeSendAnalysis(analysis, metrics, source, analysisToken);
  }

  async function maybeSendAnalysis(analysis, metrics, source, analysisToken) {
    const now = Date.now();
    const forceFreshSuggestion =
      isStillImageSource(source) || dom.results.classList.contains("d-none");
    const profile = getSelectedProfile();

    if (!profile.ready) {
      setStatus(
        "Chưa chọn giới tính/độ tuổi. AI vẫn nhận diện khuôn mặt nhưng chưa gợi ý kiểu tóc.",
        "loading",
      );
      return;
    }

    if (state.isSending) {
      if (forceFreshSuggestion) {
        state.queuedAnalysisRequest = {
          analysis,
          metrics,
          source,
          analysisToken,
        };
      }
      return;
    }

    const shouldSend =
      forceFreshSuggestion ||
      analysis.shape !== state.lastSentShape ||
      now - state.lastSentAt >= UPDATE_COOLDOWN_MS;

    if (!shouldSend) {
      return;
    }

    state.isSending = true;

    try {
      const response = await fetch(appRoot.dataset.analyzeUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          detectedShape: analysis.shape,
          gender: profile.gender,
          ageGroup: profile.ageGroup,
          confidence: analysis.confidence,
          faceLengthRatio: metrics.lengthToCheekRatio,
          foreheadWidthRatio: metrics.foreheadToCheekRatio,
          jawWidthRatio: metrics.jawToCheekRatio,
          foreheadJawDelta: metrics.foreheadJawDelta,
        }),
      });

      if (!response.ok) {
        throw new Error("The server could not prepare hairstyle suggestions.");
      }

      const payload = await response.json();
      if (analysisToken !== state.activeAnalysisToken) {
        return;
      }

      renderSuggestions(payload);
      state.lastSentShape = analysis.shape;
      state.lastSentAt = now;
    } catch (error) {
      console.error(error);
      setStatus(
        "AI đọc được khuôn mặt nhưng chưa lấy được gợi ý từ server. Hãy thử lại.",
        "error",
      );
    } finally {
      state.isSending = false;
      if (state.queuedAnalysisRequest) {
        const queuedRequest = state.queuedAnalysisRequest;
        state.queuedAnalysisRequest = null;
        maybeSendAnalysis(
          queuedRequest.analysis,
          queuedRequest.metrics,
          queuedRequest.source,
          queuedRequest.analysisToken,
        );
      }
    }
  }

  async function saveFeedback(correctedShape) {
    if (state.isSavingFeedback || state.batch.isBusy) {
      return;
    }

    if (!correctedShape) {
      setFeedbackStatus(
        "Bạn cần chọn nhãn đúng trước khi lưu feedback.",
        "error",
      );
      return;
    }

    if (!state.lastAnalysis || !state.lastMetrics) {
      setFeedbackStatus("Chưa có kết quả AI nào để lưu feedback.", "error");
      return;
    }

    state.isSavingFeedback = true;
    setFeedbackButtonsDisabled(true);
    setBatchButtonsDisabled(true);
    setFeedbackStatus("Đang lưu feedback vào dataset...", "loading");
    let shouldAdvanceBatch = false;

    try {
      const payload = await postCurrentFeedbackSample(correctedShape);

      setFeedbackStatus(payload.message, "success");
      setDatasetStatus(
        `Dataset hiện có ${payload.totalSamples} mẫu.`,
        "success",
      );

      if (state.batch.active) {
        state.batch.labeledCount += 1;
        state.batch.index += 1;
        shouldAdvanceBatch = true;
        setBatchStatus(
          `Đã lưu ${state.batch.currentFileName}. Đang mở ảnh tiếp theo...`,
          "success",
        );
        updateBatchUi();
      }
    } catch (error) {
      console.error(error);
      setFeedbackStatus("Lưu feedback thất bại. Hãy thử lại.", "error");
    } finally {
      state.isSavingFeedback = false;
      setFeedbackButtonsDisabled(false);
      setBatchButtonsDisabled(false);

      if (shouldAdvanceBatch) {
        await loadCurrentBatchItem();
      }
    }
  }

  async function postCurrentFeedbackSample(correctedShape) {
    const profile = getSelectedProfile();

    const response = await fetch(appRoot.dataset.feedbackUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        predictedShape: state.lastAnalysis.shape,
        correctedShape,
        gender: profile.gender,
        ageGroup: profile.ageGroup,
        confidence: state.lastAnalysis.confidence,
        faceLengthRatio: state.lastMetrics.lengthToCheekRatio,
        foreheadWidthRatio: state.lastMetrics.foreheadToCheekRatio,
        jawWidthRatio: state.lastMetrics.jawToCheekRatio,
        foreheadJawDelta: state.lastMetrics.foreheadJawDelta,
        detectionSource: state.lastDetectionSource || "unknown",
        modelVersion: getActiveModelVersion(),
        clientSessionId: state.clientSessionId,
        snapshotDataUrl: captureSnapshotDataUrl(),
        landmarksJson: state.lastLandmarksJson,
      }),
    });

    const payload = await response.json();
    if (!response.ok) {
      throw new Error(payload.message || "Không lưu được feedback.");
    }

    return payload;
  }

  function exportDataset() {
    setDatasetStatus("Đang tải file zip dataset...", "success");
    window.location.href = appRoot.dataset.exportFeedbackUrl;
  }

  async function importDataset(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    const formData = new FormData();
    formData.append("datasetArchive", file);
    setDatasetStatus("Đang import dataset từ file zip...", "loading");

    try {
      const response = await fetch(appRoot.dataset.importFeedbackUrl, {
        method: "POST",
        body: formData,
      });

      const payload = await response.json();
      if (!response.ok) {
        throw new Error(payload.message || "Không import được dataset.");
      }

      const runtimeModel = await loadRuntimeModel({ silent: true });
      setDatasetStatus(
        runtimeModel
          ? `${payload.message} Model đã train cũng đã được nạp trên máy này.`
          : payload.message,
        "success",
      );
      if (typeof payload.totalSamples === "number") {
        setFeedbackStatus(
          `Tổng dataset hiện tại: ${payload.totalSamples} mẫu.`,
          "success",
        );
      }
    } catch (error) {
      console.error(error);
      setDatasetStatus(
        "Import dataset thất bại. Hãy kiểm tra file zip và thử lại.",
        "error",
      );
    } finally {
      event.target.value = "";
    }
  }

  async function trainModel() {
    if (!dom.trainModelButton) {
      return;
    }

    dom.trainModelButton.disabled = true;
    setDatasetStatus("Đang train model từ feedback local...", "loading");

    try {
      const response = await fetch(appRoot.dataset.trainModelUrl, {
        method: "POST",
      });

      const payload = await response.json();
      if (!response.ok) {
        throw new Error(payload.message || "Không train được model.");
      }

      if (payload.modelCreated) {
        const runtimeModel = await loadRuntimeModel({ silent: true });
        setDatasetStatus(
          runtimeModel
            ? `${payload.message} Model mới sẽ được áp dụng ngay cho lần quét tiếp theo.`
            : payload.message,
          "success",
        );
        setFeedbackStatus("AI đã nạp model mới từ dataset local.", "success");
      } else {
        setDatasetStatus(payload.message, "error");
      }
    } catch (error) {
      console.error(error);
      setDatasetStatus(
        "Train model thất bại. Hãy kiểm tra dataset rồi thử lại.",
        "error",
      );
    } finally {
      dom.trainModelButton.disabled = false;
    }
  }

  async function handleBatchFolderSelect(event) {
    const files = Array.from(event.target.files || []).filter(
      isSupportedImageFile,
    );
    event.target.value = "";

    if (!files.length) {
      setBatchStatus("Folder này chưa có ảnh hợp lệ để gán nhãn.", "error");
      return;
    }

    if (!state.faceLandmarker) {
      setBatchStatus(
        "Mô hình AI chưa sẵn sàng. Hãy đợi thêm một chút rồi thử lại.",
        "error",
      );
      return;
    }

    cleanupCamera();
    clearImageState();

    state.batch.active = true;
    state.batch.files = files;
    state.batch.index = 0;
    state.batch.labeledCount = 0;
    state.batch.skippedCount = 0;
    state.batch.autoSkippedCount = 0;
    state.batch.currentFileName = "";
    state.batch.isBusy = false;

    if (dom.batchFolderInput) {
      dom.batchFolderInput.value = "";
    }

    updateBatchUi();
    setBatchStatus(
      `Đã nạp ${files.length} ảnh. Đang mở ảnh đầu tiên...`,
      "loading",
    );

    await loadCurrentBatchItem();
  }

  async function skipCurrentBatchImage() {
    if (!state.batch.active || state.batch.isBusy) {
      return;
    }

    state.batch.skippedCount += 1;
    state.batch.index += 1;
    setBatchStatus(
      `Đã bỏ qua ${state.batch.currentFileName || "ảnh hiện tại"}. Đang mở ảnh tiếp theo...`,
      "loading",
    );
    updateBatchUi();
    await loadCurrentBatchItem();
  }

  async function loadCurrentBatchItem() {
    if (!state.batch.active || !state.faceLandmarker) {
      return;
    }

    state.batch.isBusy = true;
    updateBatchUi();
    setFeedbackButtonsDisabled(true);

    try {
      while (
        state.batch.active &&
        state.batch.index < state.batch.files.length
      ) {
        const file = state.batch.files[state.batch.index];
        state.batch.currentFileName = file.name;
        updateBatchUi();
        clearFeedbackState();

        setBatchStatus(
          `Đang phân tích ảnh ${state.batch.index + 1}/${state.batch.files.length}: ${file.name}`,
          "loading",
        );

        await loadFileIntoStage(file, {
          hintTextFactory: (image) =>
            `Batch ${state.batch.index + 1}/${state.batch.files.length}: ${file.name} | ${image.naturalWidth} x ${image.naturalHeight}`,
          enableAnalyzeButton: false,
        });

        await ensureMode("IMAGE");
        const detection = state.faceLandmarker.detect(state.image);
        const faces = detection.faceLandmarks ?? [];

        if (faces.length === 1) {
          await handleDetectionResult(detection, "batch");
          setBatchStatus(
            `Sẵn sàng gán nhãn ảnh ${state.batch.index + 1}/${state.batch.files.length}: ${file.name}`,
            "ready",
          );
          updateBatchUi();
          return;
        }

        state.batch.skippedCount += 1;
        state.batch.autoSkippedCount += 1;
        state.batch.index += 1;
        resetMetrics();

        if (faces.length === 0) {
          setBatchStatus(
            `Bỏ qua ${file.name} vì không tìm thấy khuôn mặt rõ.`,
            "error",
          );
        } else {
          setBatchStatus(
            `Bỏ qua ${file.name} vì ảnh có nhiều hơn một khuôn mặt.`,
            "error",
          );
        }

        updateBatchUi();
      }

      if (state.batch.active) {
        const total = state.batch.files.length;
        const saved = state.batch.labeledCount;
        const skipped = state.batch.skippedCount;
        stopBatchSession({
          message: `Đã xử lý xong ${total} ảnh. Đã lưu ${saved}, bỏ qua ${skipped}.`,
          type: "success",
        });
      }
    } catch (error) {
      console.error(error);
      setBatchStatus(
        "Không đọc được một ảnh trong batch. Bạn có thể chọn folder khác.",
        "error",
      );
    } finally {
      state.batch.isBusy = false;
      updateBatchUi();

      if (state.batch.active) {
        setFeedbackButtonsDisabled(false);
      }
    }
  }

  function stopBatchSession(options = {}) {
    const {
      silent = false,
      message = "Đã dừng phiên gán nhãn hàng loạt.",
      type = "success",
    } = options;

    const hadBatch = state.batch.active || state.batch.files.length > 0;

    state.batch.active = false;
    state.batch.files = [];
    state.batch.index = 0;
    state.batch.labeledCount = 0;
    state.batch.skippedCount = 0;
    state.batch.autoSkippedCount = 0;
    state.batch.currentFileName = "";
    state.batch.isBusy = false;

    updateBatchUi();

    if (!silent && hadBatch) {
      setBatchStatus(message, type);
    } else if (!state.batch.active) {
      setBatchStatus(
        "Sau khi chọn folder, bạn chỉ cần bấm đúng nhãn hoặc sửa nhãn ở khung feedback.",
      );
    }
  }

  async function ensureMode(mode) {
    if (!state.faceLandmarker || state.runningMode === mode) {
      return;
    }

    await state.faceLandmarker.setOptions({ runningMode: mode });
    state.runningMode = mode;
  }

  function resetAnalysis() {
    stopBatchSession({ silent: true });
    cleanupCamera();
    clearImageState();
    clearCanvas();
    dom.emptyState.classList.remove("d-none");
    dom.uploadHint.textContent =
      "Bấm mở webcam để quét trực tiếp, hoặc tải ảnh chân dung một người nếu cần tư vấn thủ công.";
    dom.results.classList.add("d-none");
    resetMetrics();
    state.lastSentShape = "";
    state.lastSentAt = 0;
    clearFeedbackState();
    updateAnalysisControls();
    setStatus(
      "Đã làm mới vùng quét. Bạn có thể mở webcam hoặc chọn ảnh khác.",
      "ready",
    );
  }

  function clearImageState() {
    state.image = null;
    dom.fileInput.value = "";
    updateAnalysisControls();
    clearFeedbackState();

    if (state.objectUrl) {
      URL.revokeObjectURL(state.objectUrl);
      state.objectUrl = null;
    }
  }

  function loadFileIntoStage(file, options = {}) {
    const {
      hintTextFactory = (image) =>
        `${file.name} | ${image.naturalWidth} x ${image.naturalHeight}`,
      enableAnalyzeButton = true,
    } = options;

    clearFeedbackState();

    if (state.objectUrl) {
      URL.revokeObjectURL(state.objectUrl);
      state.objectUrl = null;
    }

    return new Promise((resolve, reject) => {
      const objectUrl = URL.createObjectURL(file);
      const image = new Image();

      state.objectUrl = objectUrl;

      image.onload = () => {
        state.image = image;
        dom.video.classList.add("is-hidden");
        dom.canvas.classList.remove("is-mirrored");
        dom.emptyState.classList.add("d-none");
        dom.uploadHint.textContent = hintTextFactory(image);
        drawImageToCanvas();
        updateAnalysisControls();
        dom.analyzeButton.disabled =
          !enableAnalyzeButton || !state.faceLandmarker;
        resolve(image);
      };

      image.onerror = () => {
        if (state.objectUrl === objectUrl) {
          URL.revokeObjectURL(objectUrl);
          state.objectUrl = null;
        }

        reject(new Error(`Cannot load image file: ${file.name}`));
      };

      image.src = objectUrl;
    });
  }

  function resizeCanvasToVideo() {
    const { videoWidth, videoHeight } = dom.video;

    if (!videoWidth || !videoHeight) {
      return;
    }

    if (dom.canvas.width !== videoWidth || dom.canvas.height !== videoHeight) {
      dom.canvas.width = videoWidth;
      dom.canvas.height = videoHeight;
    }
  }

  function drawImageToCanvas() {
    if (!state.image) {
      return;
    }

    dom.video.classList.add("is-hidden");
    dom.canvas.width = state.image.naturalWidth;
    dom.canvas.height = state.image.naturalHeight;

    const context = dom.canvas.getContext("2d");
    context.clearRect(0, 0, dom.canvas.width, dom.canvas.height);
    context.drawImage(state.image, 0, 0, dom.canvas.width, dom.canvas.height);
  }

  function clearCanvas(source = "image") {
    const context = dom.canvas.getContext("2d");
    context.clearRect(0, 0, dom.canvas.width, dom.canvas.height);

    if (isStillImageSource(source) && state.image) {
      drawImageToCanvas();
    }
  }

  function drawOverlay(landmarks, source) {
    if (isStillImageSource(source)) {
      drawImageToCanvas();
    } else {
      clearCanvas("video");
    }

    const context = dom.canvas.getContext("2d");
    const drawingUtils = new DrawingUtils(context);

    drawingUtils.drawConnectors(
      landmarks,
      FaceLandmarker.FACE_LANDMARKS_TESSELATION,
      {
        color: "rgba(212, 175, 55, 0.24)",
        lineWidth: 1,
      },
    );

    drawingUtils.drawConnectors(
      landmarks,
      FaceLandmarker.FACE_LANDMARKS_FACE_OVAL,
      {
        color: "#f4c95d",
        lineWidth: 2.2,
      },
    );

    for (const index of faceOvalIndices) {
      const point = landmarks[index];
      context.beginPath();
      context.arc(
        point.x * dom.canvas.width,
        point.y * dom.canvas.height,
        2.2,
        0,
        Math.PI * 2,
      );
      context.fillStyle = "#fff1b8";
      context.fill();
    }
  }

  function measureFace(landmarks) {
    const ovalPoints = faceOvalIndices.map((index) => landmarks[index]);

    const minY = Math.min(...ovalPoints.map((point) => point.y));
    const maxY = Math.max(...ovalPoints.map((point) => point.y));
    const faceLength = maxY - minY;
    const cheekboneWidth = spanForBand(ovalPoints, minY, maxY, 0.35, 0.6);
    const foreheadWidth = spanForBand(ovalPoints, minY, maxY, 0.08, 0.26);
    const jawWidth = spanForBand(ovalPoints, minY, maxY, 0.72, 0.92);

    return {
      faceLength,
      cheekboneWidth,
      foreheadWidth,
      jawWidth,
      lengthToCheekRatio: faceLength / cheekboneWidth,
      foreheadToCheekRatio: foreheadWidth / cheekboneWidth,
      jawToCheekRatio: jawWidth / cheekboneWidth,
      foreheadJawDelta: Math.abs(foreheadWidth - jawWidth) / cheekboneWidth,
    };
  }

  function spanForBand(points, minY, maxY, startRatio, endRatio) {
    const bandStart = minY + (maxY - minY) * startRatio;
    const bandEnd = minY + (maxY - minY) * endRatio;
    const bandPoints = points.filter(
      (point) => point.y >= bandStart && point.y <= bandEnd,
    );
    const source = bandPoints.length >= 2 ? bandPoints : points;
    const xs = source.map((point) => point.x);
    return Math.max(...xs) - Math.min(...xs);
  }

  function classifyFace(metrics) {
    const runtimePrediction = classifyFaceWithRuntimeModel(
      metrics,
      state.runtimeModel,
    );
    if (runtimePrediction) {
      return runtimePrediction;
    }

    return classifyFaceWithRules(metrics);
  }

  function classifyFaceWithRules(metrics) {
    const {
      lengthToCheekRatio,
      jawToCheekRatio,
      foreheadToCheekRatio,
      foreheadJawDelta,
    } = metrics;
    let shape = "Trái xoan";
    let confidence = 0.72;

    const isSquareLike =
      foreheadJawDelta <= 0.2 &&
      jawToCheekRatio >= 0.7 &&
      foreheadToCheekRatio >= 0.72 &&
      lengthToCheekRatio <= 1.95;

    const isLongLike =
      !isSquareLike &&
      lengthToCheekRatio >= 1.72 &&
      (jawToCheekRatio < 0.78 ||
        foreheadToCheekRatio < 0.8 ||
        foreheadJawDelta > 0.14);

    if (isSquareLike) {
      shape = "Vuông";
      confidence =
        0.74 +
        Math.min(0.08, (0.2 - foreheadJawDelta) * 0.25) +
        Math.min(0.06, Math.max(0, jawToCheekRatio - 0.7) * 0.3) +
        Math.min(0.04, Math.max(0, foreheadToCheekRatio - 0.72) * 0.2);
    } else if (isLongLike) {
      shape = "Dài";
      confidence =
        0.74 +
        Math.min(0.12, Math.max(0, lengthToCheekRatio - 1.72) * 0.35) +
        Math.min(0.05, Math.max(0, 0.8 - foreheadToCheekRatio) * 0.25);
    } else if (
      lengthToCheekRatio < 1.42 &&
      jawToCheekRatio > 0.76 &&
      foreheadToCheekRatio > 0.76
    ) {
      shape = "Tròn";
      confidence = 0.72 + Math.min(0.16, (1.42 - lengthToCheekRatio) * 0.5);
    } else {
      shape = "Trái xoan";
      confidence =
        0.7 +
        Math.min(0.15, Math.abs(foreheadToCheekRatio - jawToCheekRatio) * 0.2);
    }

    return {
      shape,
      confidence: Math.max(0.58, Math.min(0.94, confidence)),
    };
  }

  function renderQuickMetrics(analysis, metrics) {
    dom.metricShape.textContent = analysis.shape;
    dom.metricConfidence.textContent = formatPercent(analysis.confidence);
    dom.metricRatio.textContent = metrics.lengthToCheekRatio.toFixed(2);
  }

  function classifyFaceWithRuntimeModel(metrics, runtimeModel) {
    if (
      !runtimeModel ||
      !Array.isArray(runtimeModel.classes) ||
      runtimeModel.classes.length < 2
    ) {
      return null;
    }

    const featureVector = [
      metrics.lengthToCheekRatio,
      metrics.foreheadToCheekRatio,
      metrics.jawToCheekRatio,
      metrics.foreheadJawDelta,
    ];

    const means = Array.isArray(runtimeModel.globalMeans)
      ? runtimeModel.globalMeans
      : [];
    const stdDevs = Array.isArray(runtimeModel.globalStdDevs)
      ? runtimeModel.globalStdDevs
      : [];
    if (
      means.length !== featureVector.length ||
      stdDevs.length !== featureVector.length
    ) {
      return null;
    }

    const standardized = featureVector.map((value, index) => {
      const mean = Number(means[index] || 0);
      const stdDev = Math.max(Number(stdDevs[index] || 0), 0.0001);
      return (value - mean) / stdDev;
    });

    const ranked = runtimeModel.classes
      .map((entry) => {
        if (
          !Array.isArray(entry.centroid) ||
          entry.centroid.length !== standardized.length
        ) {
          return null;
        }

        const distance = Math.sqrt(
          standardized.reduce((total, value, index) => {
            const delta = value - Number(entry.centroid[index] || 0);
            return total + delta * delta;
          }, 0),
        );

        return {
          shape: normalizeShapeLabel(entry.shape),
          distance,
        };
      })
      .filter(Boolean)
      .sort((left, right) => left.distance - right.distance);

    if (ranked.length < 2 || !ranked[0]?.shape) {
      return null;
    }

    const weights = ranked.map((item) => Math.exp(-item.distance * 1.35));
    const totalWeight = weights.reduce((sum, weight) => sum + weight, 0) || 1;
    const topProbability = weights[0] / totalWeight;
    const secondProbability = weights[1] / totalWeight;
    const confidence = Math.max(
      0.58,
      Math.min(
        0.96,
        0.56 +
          topProbability * 0.24 +
          (topProbability - secondProbability) * 0.32,
      ),
    );

    return {
      shape: ranked[0].shape,
      confidence,
    };
  }

  function resetMetrics() {
    dom.metricShape.textContent = "--";
    dom.metricConfidence.textContent = "--";
    dom.metricRatio.textContent = "--";
  }

  function clearFeedbackState() {
    state.lastAnalysis = null;
    state.lastMetrics = null;
    state.lastDetectionSource = "";
    state.lastLandmarksJson = "";
    state.queuedAnalysisRequest = null;
    state.activeAnalysisToken = ++state.analysisSequence;
    dom.feedbackPanel?.classList.add("d-none");
    updateFeedbackButtonLabels();
    setFeedbackButtonsDisabled(false);
    setFeedbackStatus(
      state.batch.active
        ? "Batch đang chờ AI phân tích ảnh hiện tại để bạn xác nhận nhãn."
        : "Kết quả AI mới nhất sẽ được lưu vào dataset khi bạn xác nhận.",
    );
  }

  function setFeedbackButtonsDisabled(disabled) {
    if (dom.feedbackAcceptButton) {
      dom.feedbackAcceptButton.disabled = disabled;
    }

    dom.feedbackShapeButtons.forEach((button) => {
      button.disabled = disabled;
    });
  }

  function setBatchButtonsDisabled(disabled) {
    if (dom.selectBatchFolderButton) {
      dom.selectBatchFolderButton.disabled = disabled || !state.faceLandmarker;
    }

    if (dom.skipBatchImageButton) {
      dom.skipBatchImageButton.disabled = disabled || !state.batch.active;
    }

    if (dom.stopBatchButton) {
      dom.stopBatchButton.disabled = disabled || !state.batch.active;
    }
  }

  function setFeedbackStatus(message, type) {
    if (!dom.feedbackStatus) {
      return;
    }

    dom.feedbackStatus.textContent = message;

    if (type) {
      dom.feedbackStatus.dataset.state = type;
    } else {
      dom.feedbackStatus.removeAttribute("data-state");
    }
  }

  function setDatasetStatus(message, type) {
    if (!dom.datasetStatus) {
      return;
    }

    dom.datasetStatus.textContent = message;

    if (type) {
      dom.datasetStatus.dataset.state = type;
    } else {
      dom.datasetStatus.removeAttribute("data-state");
    }
  }

  function setBatchStatus(message, type) {
    if (!dom.batchStatus) {
      return;
    }

    dom.batchStatus.textContent = message;

    if (type) {
      dom.batchStatus.dataset.state = type;
    } else {
      dom.batchStatus.removeAttribute("data-state");
    }
  }

  function initializeFeedbackButtonLabels() {
    if (
      dom.feedbackAcceptButton &&
      !dom.feedbackAcceptButton.dataset.defaultLabel
    ) {
      dom.feedbackAcceptButton.dataset.defaultLabel =
        dom.feedbackAcceptButton.textContent.trim();
    }

    dom.feedbackShapeButtons.forEach((button) => {
      if (!button.dataset.defaultLabel) {
        button.dataset.defaultLabel = button.textContent.trim();
      }
    });

    updateFeedbackButtonLabels();
  }

  function updateFeedbackButtonLabels() {
    if (dom.feedbackAcceptButton) {
      dom.feedbackAcceptButton.textContent = state.batch.active
        ? "Đúng nhãn này & tiếp"
        : dom.feedbackAcceptButton.dataset.defaultLabel || "Kết quả đúng";
    }

    dom.feedbackShapeButtons.forEach((button) => {
      if (state.batch.active) {
        button.textContent = `${normalizeShapeLabel(button.dataset.shape || "")} & tiếp`;
      } else {
        button.textContent = button.dataset.defaultLabel || button.textContent;
      }
    });
  }

  function updateBatchUi() {
    updateFeedbackButtonLabels();

    if (dom.batchProgress) {
      if (state.batch.active && state.batch.files.length > 0) {
        const current = Math.min(
          state.batch.index + 1,
          state.batch.files.length,
        );
        dom.batchProgress.textContent = `Ảnh ${current}/${state.batch.files.length} | Đã lưu ${state.batch.labeledCount} | Bỏ qua ${state.batch.skippedCount}`;
      } else {
        dom.batchProgress.textContent = "Chưa có phiên gán nhãn hàng loạt nào.";
      }
    }

    if (dom.selectBatchFolderButton) {
      dom.selectBatchFolderButton.textContent = state.batch.active
        ? "Chọn thư mục khác"
        : "Chọn thư mục ảnh";
    }

    setBatchButtonsDisabled(state.batch.isBusy);
  }

  function buildFeedbackPrompt(predictedShape) {
    if (state.batch.active) {
      return `Ảnh ${Math.min(state.batch.index + 1, state.batch.files.length)}/${state.batch.files.length}: ${state.batch.currentFileName}. AI đang đoán ${predictedShape}. Bấm đúng nhãn hoặc sửa nhãn để sang ảnh tiếp theo.`;
    }

    return `AI đang đoán ${predictedShape}. Nếu cần, bạn có thể sửa lại nhãn đúng để lưu vào dataset.`;
  }

  function isStillImageSource(source) {
    return source === "image" || source === "batch";
  }

  function isSupportedImageFile(file) {
    if (!file) {
      return false;
    }

    return (
      file.type.startsWith("image/") ||
      /\.(png|jpe?g|webp|bmp|gif)$/i.test(file.name)
    );
  }

  function handleProfileSelectionChange() {
    state.selectedGender = normalizeGenderValue(dom.genderSelect?.value || "");
    state.selectedAgeGroup = normalizeAgeValue(dom.ageGroupSelect?.value || "");
    updateAnalysisControls();
    updateManualFallbackLinks();
    updateProfileHint();

    if (!isProfileSelectionReady()) {
      setStatus(
        "Vui lòng chọn giới tính và nhóm tuổi để AI tối ưu gợi ý.",
        "error",
      );
      return;
    }

    setStatus(
      "Thông tin hồ sơ đã cập nhật. Bạn có thể tiếp tục phân tích.",
      "ready",
    );
  }

  function getSelectedProfile() {
    const ready = isProfileSelectionReady();

    return {
      gender: state.selectedGender,
      ageGroup: state.selectedAgeGroup,
      ready,
    };
  }

  function isProfileSelectionReady() {
    return Boolean(state.selectedGender && state.selectedAgeGroup);
  }

  function updateAnalysisControls() {
    const profileReady = isProfileSelectionReady();

    if (dom.analyzeButton) {
      dom.analyzeButton.disabled = !state.faceLandmarker || !state.image;
    }

    if (dom.startCameraButton) {
      dom.startCameraButton.disabled =
        !state.faceLandmarker || state.isCameraRunning;
    }
  }

  function updateProfileHint() {
    if (!dom.profileHint) {
      return;
    }

    dom.profileHint.textContent = isProfileSelectionReady()
      ? `AI sẽ dùng hồ sơ ${state.selectedGender} • ${state.selectedAgeGroup} để điều chỉnh gợi ý.`
      : "Vui lòng chọn giới tính và nhóm tuổi trước khi phân tích để AI tối ưu gợi ý.";
  }

  function updateManualFallbackLinks() {
    const profile = getSelectedProfile();

    document.querySelectorAll(".manual-shape-link").forEach((link) => {
      const shape = link.dataset.shape || "";
      const query = new URLSearchParams({
        shape,
        gender: profile.gender,
        ageGroup: profile.ageGroup,
      });
      link.href = `${appRoot.dataset.analyzeUrl.replace("/Analyze", "/Result")}?${query.toString()}`;
    });
  }

  function formatProfileSummary(gender, ageGroup) {
    const validGender = normalizeGenderValue(gender || "");
    const validAgeGroup = normalizeAgeValue(ageGroup || "");

    if (!validGender || !validAgeGroup) {
      return "Chưa chọn giới tính/độ tuổi";
    }

    return `Hồ sơ: ${validGender} • ${validAgeGroup}`;
  }

  function normalizeGenderValue(value) {
    const normalized = String(value || "")
      .trim()
      .toLowerCase();

    if (normalized === "nam") {
      return "Nam";
    }

    if (normalized === "nữ" || normalized === "nu") {
      return "Nữ";
    }

    return "";
  }

  function normalizeAgeValue(value) {
    const normalized = String(value || "")
      .trim()
      .toLowerCase();

    if (
      normalized === "dưới 18" ||
      normalized === "duoi 18" ||
      normalized === "under 18"
    ) {
      return "Dưới 18";
    }

    if (normalized === "18-30" || normalized === "18 30") {
      return "18-30";
    }

    if (normalized === "31-45" || normalized === "31 45") {
      return "31-45";
    }

    if (
      normalized === "46+" ||
      normalized === "46" ||
      normalized === "46 trở lên"
    ) {
      return "46+";
    }

    return "";
  }

  function captureSnapshotDataUrl() {
    if (state.image) {
      const snapshotCanvas = document.createElement("canvas");
      snapshotCanvas.width = state.image.naturalWidth;
      snapshotCanvas.height = state.image.naturalHeight;
      const context = snapshotCanvas.getContext("2d");
      context.drawImage(
        state.image,
        0,
        0,
        snapshotCanvas.width,
        snapshotCanvas.height,
      );
      return snapshotCanvas.toDataURL("image/jpeg", 0.92);
    }

    if (
      state.isCameraRunning &&
      dom.video.readyState >= 2 &&
      dom.video.videoWidth &&
      dom.video.videoHeight
    ) {
      const snapshotCanvas = document.createElement("canvas");
      snapshotCanvas.width = dom.video.videoWidth;
      snapshotCanvas.height = dom.video.videoHeight;
      const context = snapshotCanvas.getContext("2d");

      context.save();
      if (dom.video.classList.contains("is-mirrored")) {
        context.translate(snapshotCanvas.width, 0);
        context.scale(-1, 1);
      }
      context.drawImage(
        dom.video,
        0,
        0,
        snapshotCanvas.width,
        snapshotCanvas.height,
      );
      context.restore();

      return snapshotCanvas.toDataURL("image/jpeg", 0.9);
    }

    if (dom.canvas.width && dom.canvas.height) {
      return dom.canvas.toDataURL("image/png");
    }

    return null;
  }

  function serializeLandmarks(landmarks) {
    return JSON.stringify(
      landmarks.map((point) => ({
        x: Number(point.x.toFixed(6)),
        y: Number(point.y.toFixed(6)),
        z: Number((point.z ?? 0).toFixed(6)),
      })),
    );
  }

  function renderSuggestions(payload) {
    dom.resultTitle.textContent = `AI gợi ý kiểu tóc cho khuôn mặt ${payload.faceShape}`;
    dom.resultSummary.textContent = payload.summary;
    dom.resultConfidence.textContent = `Độ tự tin: ${formatPercent(payload.confidence)}`;
    dom.resultMeta.textContent = `${payload.confidenceLabel} • ${formatProfileSummary(payload.gender, payload.ageGroup)}`;

    dom.tips.innerHTML = "";
    for (const tip of payload.stylingTips) {
      const item = document.createElement("li");
      item.textContent = tip;
      dom.tips.appendChild(item);
    }

    dom.cards.innerHTML = "";
    for (const suggestion of payload.suggestions) {
      const card = document.createElement("article");
      card.className = "result-card";
      const imageUrl =
        normalizeImageUrl(suggestion.imageUrl) ||
        "https://placehold.co/640x480?text=Salon+Hair";
      card.innerHTML = `
                <img class="result-card__image" src="${escapeHtml(imageUrl)}" alt="${escapeHtml(suggestion.styleName)}" onerror="this.onerror=null;this.src='https://placehold.co/640x480?text=Salon+Hair';">
                <div class="result-card__body">
                    <span class="result-card__shape">Hợp với ${escapeHtml(payload.faceShape)}</span>
                    <h3>${escapeHtml(suggestion.styleName)}</h3>
                    <p>${escapeHtml(suggestion.description || "")}</p>
                </div>`;
      dom.cards.appendChild(card);
    }

    if (payload.usedFallbackData) {
      const note = document.createElement("div");
      note.className = "alert alert-warning border-0 mt-4 mb-0";
      note.textContent =
        "Đang dùng bộ kiểu tóc gợi ý dự phòng để trải nghiệm AI không bị gián đoạn.";
      dom.cards.appendChild(note);
    }

    dom.feedbackPanel?.classList.remove("d-none");
    setFeedbackStatus(buildFeedbackPrompt(payload.faceShape));
    dom.results.classList.remove("d-none");
  }

  function setStatus(message, type) {
    dom.status.textContent = message;
    dom.status.dataset.state = type;
  }

  async function loadRuntimeModel(options = {}) {
    const { silent = false } = options;

    try {
      const response = await fetch(appRoot.dataset.runtimeModelUrl, {
        method: "GET",
        headers: {
          Accept: "application/json",
        },
      });

      if (response.status === 404) {
        state.runtimeModel = null;
        if (!silent) {
          setDatasetStatus(
            "Chưa có model train riêng. AI đang dùng thuật toán mặc định.",
            "success",
          );
        }
        return null;
      }

      if (!response.ok) {
        throw new Error("Không tải được runtime model.");
      }

      const payload = await response.json();
      state.runtimeModel = payload;

      if (!silent) {
        setDatasetStatus(
          `Đã nạp ${payload.modelVersion || "model"} train từ ${payload.totalSamples || 0} mẫu feedback local.`,
          "success",
        );
      }

      return payload;
    } catch (error) {
      console.error(error);
      state.runtimeModel = null;
      if (!silent) {
        setDatasetStatus(
          "Không tải được model train, AI sẽ dùng thuật toán mặc định.",
          "error",
        );
      }
      return null;
    }
  }

  function getActiveModelVersion() {
    return state.runtimeModel?.modelVersion || "rule-v1-local-feedback";
  }

  function normalizeShapeLabel(value) {
    const normalized = String(value || "")
      .trim()
      .toLowerCase();

    if (!normalized) return "Trái xoan";

    if (
      normalized.includes("tron") ||
      normalized.includes("tròn") ||
      normalized.includes("trã²n") ||
      normalized.includes("round")
    ) {
      return "Tròn";
    }

    if (
      normalized.includes("vuong") ||
      normalized.includes("vuông") ||
      normalized.includes("vuã´ng") ||
      normalized.includes("square")
    ) {
      return "Vuông";
    }

    if (normalized.includes("xoan") || normalized.includes("oval")) {
      return "Trái xoan";
    }

    if (
      normalized.includes("dai") ||
      normalized.includes("dài") ||
      normalized.includes("dã i") ||
      normalized.includes("long") ||
      normalized.includes("oblong")
    ) {
      return "Dài";
    }

    return "Trái xoan";
  }

  function normalizeImageUrl(imageUrl) {
    if (!imageUrl) {
      return null;
    }

    let normalized = String(imageUrl).trim();
    if (!normalized) {
      return null;
    }

    if (normalized.startsWith("~/")) {
      normalized = normalized.replace(/^~\//, "/");
    }

    if (
      !/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(normalized) &&
      !normalized.startsWith("/")
    ) {
      normalized = "/" + normalized;
    }

    return normalized;
  }

  function withTimeout(promise, timeoutMs, errorMessage) {
    return Promise.race([
      promise,
      new Promise((_, reject) => {
        window.setTimeout(() => reject(new Error(errorMessage)), timeoutMs);
      }),
    ]);
  }

  function getClientSessionId() {
    const storageKey = "salonhair-ai-feedback-session-id";

    try {
      const existingId = window.localStorage.getItem(storageKey);
      if (existingId) {
        return existingId;
      }

      const nextId =
        typeof crypto !== "undefined" && typeof crypto.randomUUID === "function"
          ? crypto.randomUUID()
          : `session-${Date.now()}`;

      window.localStorage.setItem(storageKey, nextId);
      return nextId;
    } catch {
      return `session-${Date.now()}`;
    }
  }

  function formatPercent(value) {
    return `${Math.round(value * 100)}%`;
  }

  function escapeHtml(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }
}
