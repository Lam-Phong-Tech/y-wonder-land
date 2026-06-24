from PIL import Image
import sys

def remove_white_bg(image_path, output_path, threshold=240):
    try:
        img = Image.open(image_path).convert("RGBA")
        data = img.getdata()

        new_data = []
        for item in data:
            # Check if pixel is close to white
            if item[0] > threshold and item[1] > threshold and item[2] > threshold:
                new_data.append((255, 255, 255, 0))
            else:
                new_data.append(item)

        img.putdata(new_data)
        img.save(output_path, "PNG")
        print(f"Successfully processed {image_path} -> {output_path}")
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    remove_white_bg('Logo_YWonderHub.jpg', 'Logo_YWonderHub.png', 235)
