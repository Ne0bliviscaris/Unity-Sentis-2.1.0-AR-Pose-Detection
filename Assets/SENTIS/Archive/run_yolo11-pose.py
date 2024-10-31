import cv2
from ultralytics import YOLO


def process_video_stream():
    """Process video stream and overlay skeleton in real-time."""
    model_address = "Model\\yolo11n-pose.pt"
    model = YOLO(model_address)
    cap = cv2.VideoCapture(0)  # Open the default camera

    if not cap.isOpened():
        print("Error: Could not open video stream.")
        return

    inference_args = {
        "conf": 0.7,  # confidence threshold
        "iou": 0.5,  # NMS IoU threshold
        "max_det": 1,  # maximum number of detections per image
        "save": False,
        "show": False,
    }

    while True:
        ret, frame = cap.read()
        if not ret:
            break
        # Save the current frame to a temporary file
        temp_input_path = "temp_frame.jpg"
        cv2.imwrite(temp_input_path, frame)
        result = model(temp_input_path, device="cpu", **inference_args)

        # Overlay skeleton on the frame
        output_frame = result[0].plot()

        # Display the frame
        cv2.imshow("Pose Detection", output_frame)

        # Break the loop if 'q' is pressed
        if cv2.waitKey(1) & 0xFF == ord("q"):
            break

    cap.release()
    cv2.destroyAllWindows()
process_video_stream()
