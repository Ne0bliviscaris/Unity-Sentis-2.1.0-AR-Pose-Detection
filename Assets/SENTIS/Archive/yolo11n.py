import cv2
from ultralytics import YOLO

model_address = "Model\\yolo11n-pose.pt"
model = YOLO(model_address)
# cap = cv2.VideoCapture(0)  # Open the default camera


inference_args = {
    "conf": 0.7,  # confidence threshold
    "iou": 0.5,  # NMS IoU threshold
    "max_det": 1,  # maximum number of detections per image
    "save": False,
    "show": False,
    "stream": True,
}

# while True:
#     # ret, frame = cap.read()
#     if not ret:
#         break
# Save the current frame to a temporary file
# temp_input_path = "temp_frame.jpg"
# cv2.imwrite(temp_input_path, frame)
model(0, device="cpu", **inference_args)

# Overlay skeleton on the frame
# output_frame = result[0].plot()
